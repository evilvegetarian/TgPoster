import {useState} from "react";
import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import {z} from "zod";
import {toast} from "sonner";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/components/ui/card";
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Tabs, TabsContent, TabsList, TabsTrigger} from "@/components/ui/tabs";
import {Loader2, Upload, Youtube} from "lucide-react";
import {usePostApiV1Youtube} from "@/api/endpoints/you-tube-account/you-tube-account";

const fileSchema = z.object({
    file: z.instanceof(FileList).refine((files) => files.length > 0, "Файл обязателен"),
});

const manualSchema = z.object({
    clientId: z.string().min(1, "Client ID обязателен"),
    clientSecret: z.string().min(1, "Client Secret обязателен"),
});

type FileFormValues = z.infer<typeof fileSchema>;
type ManualFormValues = z.infer<typeof manualSchema>;

export function YouTubeAccountPage() {
    const [activeTab, setActiveTab] = useState("file");
    
    const {mutateAsync: authYouTube, isPending} = usePostApiV1Youtube();

    const fileForm = useForm<FileFormValues>({
        resolver: zodResolver(fileSchema),
    });

    const manualForm = useForm<ManualFormValues>({
        resolver: zodResolver(manualSchema),
        defaultValues: {
            clientId: "",
            clientSecret: "",
        },
    });

    const onFileSubmit = async (values: FileFormValues) => {
        try {
            const file = values.file[0];
            // @ts-ignore
            const response = await authYouTube({data: {JsonFile: file}});
            // @ts-ignore
            if (response?.url) {
                // @ts-ignore
                window.location.href = response.url;
            }
        } catch (error: any) {
            toast.error("Ошибка авторизации", {
                description: error.response?.data?.title || "Не удалось начать авторизацию",
            });
        }
    };

    const onManualSubmit = async (values: ManualFormValues) => {
        try {
            // @ts-ignore
            const response = await authYouTube({
                data: {
                    ClientId: values.clientId,
                    ClientSecret: values.clientSecret
                }
            });
            // @ts-ignore
            if (response?.url) {
                // @ts-ignore
                window.location.href = response.url;
            }
        } catch (error: any) {
            toast.error("Ошибка авторизации", {
                description: error.response?.data?.title || "Не удалось начать авторизацию",
            });
        }
    };

    return (
        <div className="container mx-auto py-10">
            <div className="flex items-center gap-4 mb-8">
                <Youtube className="h-8 w-8 text-red-600" />
                <h1 className="text-3xl font-bold">YouTube Аккаунты</h1>
            </div>

            <div className="grid gap-6 md:grid-cols-2">
                <Card>
                    <CardHeader>
                        <CardTitle>Добавить аккаунт</CardTitle>
                        <CardDescription>
                            Авторизуйтесь через Google, чтобы публиковать видео на YouTube.
                            Вы можете загрузить файл client_secret.json или ввести данные вручную.
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
                            <TabsList className="grid w-full grid-cols-2">
                                <TabsTrigger value="file">Загрузить файл</TabsTrigger>
                                <TabsTrigger value="manual">Вручную</TabsTrigger>
                            </TabsList>
                            
                            <TabsContent value="file">
                                <Form {...fileForm}>
                                    <form onSubmit={fileForm.handleSubmit(onFileSubmit)} className="space-y-4 pt-4">
                                        <FormField
                                            control={fileForm.control}
                                            name="file"
                                            render={({ field: { onChange, value, ...field } }) => (
                                                <FormItem>
                                                    <FormLabel>Файл client_secret.json</FormLabel>
                                                    <FormControl>
                                                        <div className="grid w-full max-w-sm items-center gap-1.5">
                                                            <Input
                                                                type="file"
                                                                accept=".json"
                                                                onChange={(e) => {
                                                                    onChange(e.target.files);
                                                                }}
                                                                {...field}
                                                            />
                                                        </div>
                                                    </FormControl>
                                                    <FormMessage />
                                                </FormItem>
                                            )}
                                        />
                                        <Button type="submit" disabled={isPending} className="w-full">
                                            {isPending ? (
                                                <>
                                                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                                    Перенаправление...
                                                </>
                                            ) : (
                                                <>
                                                    <Upload className="mr-2 h-4 w-4" />
                                                    Авторизоваться
                                                </>
                                            )}
                                        </Button>
                                    </form>
                                </Form>
                            </TabsContent>
                            
                            <TabsContent value="manual">
                                <Form {...manualForm}>
                                    <form onSubmit={manualForm.handleSubmit(onManualSubmit)} className="space-y-4 pt-4">
                                        <FormField
                                            control={manualForm.control}
                                            name="clientId"
                                            render={({ field }) => (
                                                <FormItem>
                                                    <FormLabel>Client ID</FormLabel>
                                                    <FormControl>
                                                        <Input placeholder="Ваш Client ID" {...field} />
                                                    </FormControl>
                                                    <FormMessage />
                                                </FormItem>
                                            )}
                                        />
                                        <FormField
                                            control={manualForm.control}
                                            name="clientSecret"
                                            render={({ field }) => (
                                                <FormItem>
                                                    <FormLabel>Client Secret</FormLabel>
                                                    <FormControl>
                                                        <Input type="password" placeholder="Ваш Client Secret" {...field} />
                                                    </FormControl>
                                                    <FormMessage />
                                                </FormItem>
                                            )}
                                        />
                                        <Button type="submit" disabled={isPending} className="w-full">
                                            {isPending ? (
                                                <>
                                                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                                    Перенаправление...
                                                </>
                                            ) : (
                                                "Авторизоваться"
                                            )}
                                        </Button>
                                    </form>
                                </Form>
                            </TabsContent>
                        </Tabs>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>Инструкция</CardTitle>
                        <CardDescription>
                            Как получить данные для авторизации
                        </CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4 text-sm">
                        <ol className="list-decimal list-inside space-y-2">
                            <li>Перейдите в <a href="https://console.cloud.google.com/" target="_blank" rel="noreferrer" className="text-blue-600 hover:underline">Google Cloud Console</a>.</li>
                            <li>Создайте новый проект или выберите существующий.</li>
                            <li>В меню выберите "APIs & Services" "Library" и включите "YouTube Data API v3".</li>
                            <li>Перейдите в "APIs & Services"  "Credentials".</li>
                            <li>Нажмите "Create Credentials"  "OAuth client ID".</li>
                            <li>Выберите тип приложения "Web application".</li>
                            <li>В "Authorized redirect URIs" добавьте: <code className="bg-muted px-1 py-0.5 rounded">{window.location.origin}/api/v1/youtube/callback</code></li>
                            <li>Нажмите "Create" и скачайте JSON файл или скопируйте Client ID и Secret.</li>
                        </ol>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
