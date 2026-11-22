import assert from 'node:assert/strict';
import {after, before, test} from 'node:test';
import {spawn} from 'node:child_process';
import {access, mkdtemp, readFile, rm} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {dirname, join, resolve, sep} from 'node:path';
import {fileURLToPath} from 'node:url';
import {setTimeout as delay} from 'node:timers/promises';
import {createServer, transformWithEsbuild} from 'vite';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
let temporary, server, browser, origin, browserCdp;

class Cdp {
    sequence = 0;
    pending = new Map();

    constructor(socket) {
        this.socket = socket;
        socket.addEventListener('message', event => {
            const message = JSON.parse(event.data);
            const request = this.pending.get(message.id);
            if (!request) return;
            this.pending.delete(message.id);
            if (message.error) request.reject(new Error(message.error.message));
            else request.resolve(message.result);
        });
    }

    static async connect(url) {
        const socket = new WebSocket(url);
        await new Promise((resolve, reject) => {
            socket.addEventListener('open', resolve, {once: true});
            socket.addEventListener('error', reject, {once: true});
        });
        return new Cdp(socket);
    }

    send(method, params = {}) {
        const id = ++this.sequence;
        return new Promise((resolve, reject) => {
            const timeout = setTimeout(() => {
                this.pending.delete(id);
                reject(new Error(`CDP timeout: ${method}`));
            }, 15000);
            this.pending.set(id, {
                resolve: value => { clearTimeout(timeout); resolve(value); },
                reject: error => { clearTimeout(timeout); reject(error); }
            });
            this.socket.send(JSON.stringify({id, method, params}));
        });
    }

    async evaluate(expression) {
        const result = await this.send('Runtime.evaluate', {expression, awaitPromise: true, returnByValue: true});
        if (result.exceptionDetails) throw new Error(result.exceptionDetails.exception?.description ?? result.exceptionDetails.text);
        return result.result.value;
    }

    async until(expression) {
        const deadline = Date.now() + 15000;
        while (Date.now() < deadline) {
            if (await this.evaluate(expression)) return;
            await delay(25);
        }
        throw new Error(`Browser assertion timed out: ${expression}`);
    }
}

const entry = `
import React from 'react';
import {createRoot} from 'react-dom/client';
import {MemoryRouter, Routes, Route} from 'react-router-dom';
import {AuthProvider, useAuth} from '/src/auth-context.tsx';
import {ProtectedRoute} from '/src/protected-route.tsx';
import {PublicRoute} from '/src/public-route.tsx';
import usePersistentState from '/src/pages/use-persistent-state.tsx';
import {useDebounce} from '/src/hooks/use-debounce.ts';
window.openSimilarChannels = async () => {
    const {SimilarChannelsDialog} = await import('/src/components/discover/similar-channels-dialog.tsx');
    const {QueryClient, QueryClientProvider} = await import('@tanstack/react-query');
    const root = createRoot(document.body.appendChild(document.createElement('div')));
    root.render(<QueryClientProvider client={new QueryClient()}>
        <SimilarChannelsDialog sourceId="source-id" sourceName="Источник" onClose={() => root.unmount()}/>
    </QueryClientProvider>);
};
function Probe() {
    const auth = useAuth();
    const [filter, setFilter] = usePersistentState<number | null>('filter', 1);
    const [draft, setDraft] = React.useState('initial');
    const debounced = useDebounce(draft, 100);
    return <>
        <output id="authenticated">{String(auth.isAuthenticated)}</output>
        <button id="login" onClick={() => auth.login('access', 'refresh')}>login</button>
        <button id="logout" onClick={auth.logout}>logout</button>
        <button id="refresh" onClick={async () => { window.testResult = await auth.refreshTokens(); }}>refresh</button>
        <output id="filter">{filter}</output>
        <button id="increment-filter" onClick={() => setFilter(value => (value ?? 0) + 1)}>increment</button>
        <button id="clear-filter" onClick={() => setFilter(null)}>clear</button>
        <output id="debounced">{debounced}</output>
        <button id="first-draft" onClick={() => setDraft('first')}>first</button>
        <button id="last-draft" onClick={() => setDraft('last')}>last</button>
        <Routes>
            <Route element={<PublicRoute/>}><Route path="/login" element={<div id="login-page"/>}/></Route>
            <Route element={<ProtectedRoute/>}><Route path="/" element={<div id="private-page"/>}/></Route>
        </Routes>
    </>;
}
createRoot(document.getElementById('root')).render(<AuthProvider><MemoryRouter initialEntries={[new URLSearchParams(location.search).get('route') || '/']}><Probe/></MemoryRouter></AuthProvider>);
`;

before(async () => {
    temporary = await mkdtemp(join(tmpdir(), 'tgposter-client-test-'));
    server = await createServer({
        root, configFile: false, envFile: false, appType: 'custom', logLevel: 'silent',
        cacheDir: join(temporary, 'vite'),
        resolve: {alias: {'@': join(root, 'src')}},
        esbuild: {jsx: 'automatic'},
        server: {host: '127.0.0.1', port: 0, fs: {allow: [root, temporary]}},
        optimizeDeps: {noDiscovery: true, include: ['react', 'react/jsx-runtime', 'react/jsx-dev-runtime', 'react-dom', 'react-dom/client', 'react-router-dom', 'lucide-react', 'react-hook-form', '@hookform/resolvers/zod', 'axios', '@tanstack/react-query']},
        plugins: [{
            name: 'test-fixture', enforce: 'pre',
            resolveId(source) {
                if (source.includes('/api/endpoints/account/account')) return '\0test-account';
                if (source === '/__tests__/entry.tsx') return '\0test-entry.tsx';
            },
            async load(id) {
                if (id === '\0test-account') return 'export const postApiV1AccountRefreshToken = request => window.refreshResponse(request);';
                if (id === '\0test-entry.tsx') return await transformWithEsbuild(entry, 'entry.tsx', {loader: 'tsx', jsx: 'automatic'});
            },
            configureServer(instance) {
                instance.middlewares.use((request, response, next) => {
                    if (!request.url.startsWith('/__tests__/page')) return next();
                    response.setHeader('Content-Type', 'text/html');
                    response.end(`<div id="root"></div><script>
                        localStorage.clear();
                        const q = new URLSearchParams(location.search);
                        for (const key of ['accessToken', 'refreshToken', 'filter']) if(q.has(key)) localStorage.setItem(key, q.get(key));
                        window.refreshCalls = [];
                        window.refreshResponse = async request => { window.refreshCalls.push(request); return {accessToken:'new-access', refreshToken:'new-refresh'}; };
                    </script><script type="module" src="/__tests__/entry.tsx"></script>`);
                });
            }
        }]
    });
    await server.listen();
    origin = `http://127.0.0.1:${server.httpServer.address().port}`;
    const prepared = await fetch(origin + '/__tests__/entry.tsx');
    assert.equal(prepared.status, 200, await prepared.text());
    const candidates = [process.env.CHROME_PATH,
        'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
        'C:/Program Files/Google/Chrome/Application/chrome.exe',
        '/usr/bin/google-chrome', '/usr/bin/chromium', '/usr/bin/chromium-browser'].filter(Boolean);
    let executable;
    for (const candidate of candidates) {
        if (await access(candidate).then(() => true, () => false)) { executable = candidate; break; }
    }
    assert.ok(executable, 'Chrome/Edge is required; set CHROME_PATH to an installed browser');
    const profile = join(temporary, 'browser');
    browser = spawn(executable, ['--headless=new', '--no-first-run', '--no-default-browser-check',
        '--disable-background-networking', '--disable-component-update', '--disable-sync',
        '--remote-debugging-port=0', `--user-data-dir=${profile}`, 'about:blank'], {windowsHide: true, stdio: 'ignore'});
    let endpoint;
    for (let attempt = 0; attempt < 150; attempt++) {
        endpoint = await readFile(join(profile, 'DevToolsActivePort'), 'utf8').catch(() => null);
        if (endpoint) break;
        if (browser.exitCode !== null) throw new Error(`Browser exited with ${browser.exitCode}`);
        await delay(100);
    }
    assert.ok(endpoint, 'Browser did not expose its debugging endpoint');
    const [port, path] = endpoint.trim().split(/\r?\n/);
    browserCdp = await Cdp.connect(`ws://127.0.0.1:${port}${path}`);
}, {timeout: 60000});

after(async () => {
    if (browserCdp) {
        await browserCdp.send('Browser.close').catch(() => {});
        browserCdp.socket.close();
    }
    if (browser && browser.exitCode === null) {
        await Promise.race([new Promise(resolve => browser.once('exit', resolve)), delay(5000)]);
        if (browser.exitCode === null) browser.kill();
    }
    await server?.close();
    if (temporary && resolve(temporary).startsWith(resolve(tmpdir()) + sep))
        await rm(temporary, {recursive: true, force: true, maxRetries: 10, retryDelay: 100});
});

async function page(parameters = {}) {
    const {targetId} = await browserCdp.send('Target.createTarget', {url: 'about:blank'});
    const {targetInfos} = await browserCdp.send('Target.getTargets');
    const browserUrl = browserCdp.socket.url;
    const target = targetInfos.find(target => target.targetId === targetId);
    assert.ok(target);
    const cdp = await Cdp.connect(browserUrl.replace(/\/devtools\/browser\/.*/, '/devtools/page/' + targetId));
    const errors = [];
    cdp.socket.addEventListener('message', event => {
        const message = JSON.parse(event.data);
        if (message.method === 'Runtime.exceptionThrown') errors.push(message.params.exceptionDetails.exception?.description);
        if (message.method === 'Log.entryAdded' && message.params.entry.level === 'error') errors.push(message.params.entry.text);
    });
    await cdp.send('Runtime.enable');
    await cdp.send('Log.enable');
    await cdp.send('Page.enable');
    await cdp.send('Emulation.setTimezoneOverride', {timezoneId: 'Asia/Yekaterinburg'});
    await cdp.send('Page.navigate', {url: origin + '/__tests__/page?' + new URLSearchParams(parameters)});
    try { await cdp.until('document.querySelector("#authenticated") !== null'); }
    catch (error) {
        cdp.socket.close();
        await browserCdp.send('Target.closeTarget', {targetId});
        throw new Error(`${error.message}; ${errors.join('; ')}`);
    }
    cdp.close = async () => {
        cdp.socket.close();
        await browserCdp.send('Target.closeTarget', {targetId});
    };
    return cdp;
}

test('anonymous users are redirected to login', async t => {
    const browser = await page(); t.after(() => browser.close());
    await browser.until('document.querySelector("#login-page") !== null');
    assert.equal(await browser.evaluate('document.querySelector("#authenticated").textContent'), 'false');
});

test('saved credentials restore authentication and redirect public routes', async t => {
    const browser = await page({route: '/login', accessToken: 'saved-access', refreshToken: 'saved-refresh'});
    t.after(() => browser.close());
    await browser.until('document.querySelector("#private-page") !== null');
    assert.equal(await browser.evaluate('localStorage.getItem("accessToken")'), 'saved-access');
});

test('login persists both tokens and logout removes them', async t => {
    const browser = await page(); t.after(() => browser.close());
    await browser.evaluate('document.querySelector("#login").click()');
    await browser.until('document.querySelector("#private-page") !== null && localStorage.getItem("refreshToken") === "refresh"');
    assert.equal(await browser.evaluate('localStorage.getItem("accessToken")'), 'access');
    await browser.evaluate('document.querySelector("#logout").click()');
    await browser.until('document.querySelector("#login-page") !== null && localStorage.getItem("accessToken") === null');
    assert.equal(await browser.evaluate('localStorage.getItem("refreshToken")'), null);
});

test('refresh replaces both tokens and sends the saved refresh token', async t => {
    const browser = await page({accessToken: 'old-access', refreshToken: 'old-refresh'}); t.after(() => browser.close());
    await browser.evaluate('document.querySelector("#refresh").click()');
    await browser.until('window.testResult === true && localStorage.getItem("accessToken") === "new-access"');
    assert.equal(await browser.evaluate('localStorage.getItem("refreshToken")'), 'new-refresh');
    assert.deepEqual(await browser.evaluate('window.refreshCalls'), [{refreshToken: 'old-refresh'}]);
});

for (const scenario of ['request failure', 'incomplete response', 'missing refresh token']) {
    test(`refresh logs out after ${scenario}`, async t => {
        const parameters = {accessToken: 'old-access'};
        if (scenario !== 'missing refresh token') parameters.refreshToken = 'old-refresh';
        const browser = await page(parameters); t.after(() => browser.close());
        if (scenario === 'request failure') await browser.evaluate('window.refreshResponse = async () => { throw new Error("offline"); }');
        if (scenario === 'incomplete response') await browser.evaluate('window.refreshResponse = async () => ({accessToken:"orphan"})');
        await browser.evaluate('document.querySelector("#refresh").click()');
        await browser.until('window.testResult === false && document.querySelector("#login-page") !== null');
        assert.equal(await browser.evaluate('localStorage.getItem("accessToken")'), null);
        assert.equal(await browser.evaluate('localStorage.getItem("refreshToken")'), null);
    });
}

test('time conversion respects UTC and the user timezone', async t => {
    const browser = await page(); t.after(() => browser.close());
    const values = await browser.evaluate(`(async () => {
        const time = await import('/src/utils/convertLocalToIsoTime.tsx');
        return [time.convertLocalToIsoTime('22:45'), time.convertLocalToIsoTime('invalid'),
            time.convertUtcTimeToLocalTime(''), time.utcToLocalString('2026-10-24T15:45:00Z')];
    })()`);
    assert.deepEqual(values, ['17:45', 'invalid', 'Invalid Time', '2026-10-24T20:45']);
});

test('destination actions account for membership, activation and available names', async t => {
    const browser = await page(); t.after(() => browser.close());
    const values = await browser.evaluate(`(async () => {
        const {isProblemDestination, canBeEnabled, destinationTitle} = await import('/src/pages/repostpage/repost-destination-utils.ts');
        const active = {chatId:123, chatStatus:'Active', isActive:true};
        return [isProblemDestination({...active, chatStatus:'Banned'}), isProblemDestination({...active, chatStatus:'Left'}),
            isProblemDestination(active), canBeEnabled(active), canBeEnabled({...active,isActive:false}),
            canBeEnabled({...active,chatStatus:'Banned'}), destinationTitle({...active,title:'Title',username:'user'}),
            destinationTitle({...active,username:'user'}), destinationTitle(active)];
    })()`);
    assert.deepEqual(values, [true, true, false, false, true, true, 'Title', '@user', '123']);
});

test('Discover previews recommendations and saves only the selected new channels', async t => {
    const browser = await page(); t.after(() => browser.close());
    await browser.evaluate(`(async () => {
        const {axiosInstance} = await import('/src/api/axios-instance.ts');
        const channels = [
            {telegramId: 1, title: 'Известный', discoverId: 'known-id'},
            {telegramId: 2, title: 'Первый'},
            {telegramId: 3, title: 'Второй'}
        ];
        window.savedSelections = [];
        axiosInstance.defaults.adapter = async config => {
            if (config.url !== '/api/v1/discover/source-id/similar') throw new Error(config.url);
            let data = {source: 'Telegram', channels};
            if (config.method === 'post') {
                const {telegramIds} = JSON.parse(config.data);
                window.savedSelections.push(telegramIds);
                for (const id of telegramIds) channels.find(c => c.telegramId === id).discoverId = 'saved-' + id;
                data = telegramIds.length;
            }
            return {data, status: 200, headers: {}, config};
        };
        await window.openSimilarChannels();
    })()`);
    const checkboxes = 'document.querySelectorAll("[role=dialog] [role=checkbox]")';
    const save = '[...document.querySelectorAll("[role=dialog] button")].find(b => b.textContent.includes("Сохранить"))';
    await browser.until(`${checkboxes}.length === 2`);
    assert.equal(await browser.evaluate(`${save}.disabled`), true);
    assert.deepEqual(await browser.evaluate('window.savedSelections'), []);
    await browser.evaluate(`${checkboxes}[1].click()`);
    await browser.until(`!${save}.disabled`);
    await browser.evaluate(`${save}.click()`);
    await browser.until(`${checkboxes}.length === 1`);
    assert.deepEqual(await browser.evaluate('window.savedSelections'), [[3]]);
    assert.equal(await browser.evaluate(`${checkboxes}[0].getAttribute("aria-label")`), 'Выбрать Первый');
});

test('persistent filters restore saved values, support functional updates and clear storage', async t => {
    const browser = await page({filter: '2'}); t.after(() => browser.close());
    assert.equal(await browser.evaluate('document.querySelector("#filter").textContent'), '2');
    await browser.evaluate('document.querySelector("#increment-filter").click()');
    await browser.until('document.querySelector("#filter").textContent === "3"');
    assert.equal(await browser.evaluate('localStorage.getItem("filter")'), '3');
    await browser.evaluate('document.querySelector("#clear-filter").click()');
    await browser.until('localStorage.getItem("filter") === null');
});

test('debounce publishes only the latest value after a burst of updates', async t => {
    const browser = await page(); t.after(() => browser.close());
    const immediate = await browser.evaluate('document.querySelector("#first-draft").click(); document.querySelector("#last-draft").click(); document.querySelector("#debounced").textContent');
    assert.equal(immediate, 'initial');
    await browser.until('document.querySelector("#debounced").textContent === "last"');
});

test('proxy form validates bounds and MTProxy secret and normalizes submitted fields', async t => {
    const browser = await page(); t.after(() => browser.close());
    const result = await browser.evaluate(`(async () => {
        const {proxyFormSchema} = await import('/src/components/proxy/proxy-form-fields.tsx');
        const valid = {name:' proxy ', type:'Http', host:' example.test ', port:'8080'};
        const parsed = proxyFormSchema.parse(valid);
        return {
            valid: parsed,
            emptyName: proxyFormSchema.safeParse({...valid,name:' '}).success,
            emptyHost: proxyFormSchema.safeParse({...valid,host:''}).success,
            zeroPort: proxyFormSchema.safeParse({...valid,port:0}).success,
            excessivePort: proxyFormSchema.safeParse({...valid,port:65536}).success,
            fractionalPort: proxyFormSchema.safeParse({...valid,port:80.5}).success,
            missingSecret: proxyFormSchema.safeParse({...valid,type:'MTProxy'}).success,
            withSecret: proxyFormSchema.safeParse({...valid,type:'MTProxy',secret:'test-secret'}).success,
            firstPort: proxyFormSchema.safeParse({...valid,port:1}).success,
            lastPort: proxyFormSchema.safeParse({...valid,port:65535}).success
        };
    })()`);
    assert.deepEqual(result, {
        valid: {name: 'proxy', type: 'Http', host: 'example.test', port: 8080},
        emptyName: false, emptyHost: false, zeroPort: false, excessivePort: false, fractionalPort: false,
        missingSecret: false, withSecret: true, firstPort: true, lastPort: true
    });
});

test('API requests attach the saved bearer token and unwrap data and blobs', async t => {
    const browser = await page({accessToken: 'saved-access'}); t.after(() => browser.close());
    const result = await browser.evaluate(`(async () => {
        const api = await import('/src/api/axios-instance.ts');
        const calls = [];
        api.axiosInstance.defaults.adapter = async config => {
            calls.push({token:config.headers.Authorization, responseType:config.responseType ?? null});
            return {data:config.responseType === 'blob' ? new Blob(['file']) : {id:123},status:200,headers:{},config};
        };
        const json = await api.customInstance({url:'/test'});
        const blob = await api.fileInstance({url:'/test'});
        return {json,blob:await blob.text(),calls};
    })()`);
    assert.deepEqual(result, {json:{id:123},blob:'file',calls:[
        {token:'Bearer saved-access',responseType:null},{token:'Bearer saved-access',responseType:'blob'}]});
});

test('concurrent unauthorized requests share one refresh and retry with the new token', async t => {
    const browser = await page({accessToken:'old-access',refreshToken:'old-refresh'}); t.after(() => browser.close());
    const result = await browser.evaluate(`(async () => {
        const {axiosInstance} = await import('/src/api/axios-instance.ts');
        const calls = [];
        window.refreshResponse = async request => {
            window.refreshCalls.push(request);
            await new Promise(resolve => setTimeout(resolve, 50));
            return {accessToken:'new-access',refreshToken:'new-refresh'};
        };
        axiosInstance.defaults.adapter = async config => {
            calls.push(config.headers.Authorization);
            if (config.headers.Authorization === 'Bearer old-access') throw {config,response:{status:401,data:{title:'expired'}}};
            return {data:config.url,status:200,headers:{},config};
        };
        const responses = await Promise.all(['/first','/second','/third'].map(url => axiosInstance({url})));
        return {data:responses.map(response => response.data),calls,refreshCalls:window.refreshCalls,
            access:localStorage.getItem('accessToken'),refresh:localStorage.getItem('refreshToken')};
    })()`);
    assert.deepEqual(result.data, ['/first','/second','/third']);
    assert.equal(result.calls.filter(token => token === 'Bearer old-access').length, 3);
    assert.equal(result.calls.filter(token => token === 'Bearer new-access').length, 3);
    assert.deepEqual(result.refreshCalls, [{refreshToken:'old-refresh'}]);
    assert.equal(result.access, 'new-access');
    assert.equal(result.refresh, 'new-refresh');
});

test('a retried unauthorized request stops after one refresh', async t => {
    const browser = await page({accessToken:'old-access',refreshToken:'old-refresh'}); t.after(() => browser.close());
    const result = await browser.evaluate(`(async () => {
        const {axiosInstance} = await import('/src/api/axios-instance.ts');
        let calls = 0;
        axiosInstance.defaults.adapter = async config => {
            calls++;
            throw {config,response:{status:calls > 4 ? 403 : 401,data:{title:'unauthorized'}}};
        };
        let rejected = false;
        try { await axiosInstance({url:'/denied'}); } catch { rejected = true; }
        return {calls,refreshes:window.refreshCalls.length,rejected};
    })()`);
    assert.deepEqual(result, {calls:2,refreshes:1,rejected:true});
});

test('network errors retain their original cause when no HTTP response exists', async t => {
    const browser = await page(); t.after(() => browser.close());
    const result = await browser.evaluate(`(async () => {
        const {axiosInstance} = await import('/src/api/axios-instance.ts');
        const offline = new Error('offline');
        axiosInstance.defaults.adapter = async () => { throw offline; };
        try { await axiosInstance({url:'/offline'}); } catch (error) { return error === offline; }
        return false;
    })()`);
    assert.equal(result, true);
});

test('cancelled requests return the cancellation status without refreshing tokens', async t => {
    const browser = await page(); t.after(() => browser.close());
    const result = await browser.evaluate(`(async () => {
        const {axiosInstance} = await import('/src/api/axios-instance.ts');
        axiosInstance.defaults.adapter = async config => { throw {config,code:'ERR_CANCELED'}; };
        const response = await axiosInstance({url:'/cancelled'});
        return {status:response.status,refreshes:window.refreshCalls.length};
    })()`);
    assert.deepEqual(result, {status:499,refreshes:0});
});
