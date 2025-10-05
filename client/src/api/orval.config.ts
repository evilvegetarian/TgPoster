import { defineConfig } from 'orval';

export default defineConfig({
    api: {
        output: {
            mode: 'tags-split',
            target: 'src/api/endpoints',
            client: 'react-query',
            override: {
                mutator: {
                    path: './src/api/axios-instance.ts',
                    name: 'customInstance',
                },
                query: {
                    useQuery: true,
                    useMutation: true,
                },
            },
        },
        input: {
            target:`${import.meta.env.VITE_API_URL}/swagger/v1/swagger.json`,
        },
    },
});
