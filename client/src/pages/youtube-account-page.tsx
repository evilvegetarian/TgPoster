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
import {Loader2, Upload, Trash2, Video} from "lucide-react";
import {
    usePostApiV1Youtube,
    useGetApiV1Youtube,
    useDeleteApiV1YoutubeId
} from "@/api/endpoints/you-tube-account/you-tube-account";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog";

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

    const {mutate: authYouTube, isPending} = usePostApiV1Youtube({
        mutation: {
            onSuccess: (data) => {
                window.location.href=data.url;
                refetch();
            },
            onError: (error) => {
                toast.error("Ошибка авторизации", {
                    description: error.title || "Не удалось начать авторизацию",
                });
            },
        },
    });
    const {data: accounts, isLoading, refetch} = useGetApiV1Youtube();
    const {mutate: deleteAccount} = useDeleteApiV1YoutubeId({
        mutation: {
            onSuccess: () => {
                toast.success("Аккаунт удален");
                refetch();
            },
            onError: (error) => {
                toast.error("Ошибка удаления", {
                    description: error.title || "Не удалось удалить аккаунт",
                });
            },
        },
    });

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

    const onFileSubmit = (values: FileFormValues) => {
        const file = values.file[0];
        authYouTube({data: {JsonFile: file}});
    };

    const onManualSubmit = (values: ManualFormValues) => {
        authYouTube({
            data: {
                ClientId: values.clientId,
                ClientSecret: values.clientSecret
            }
        });
    };

    const handleDelete = (id: string) => {
        deleteAccount({id});
    };

    return (
        <div className="container mx-auto py-10">
            <div className="flex items-center gap-4 mb-8">
                <Video className="h-8 w-8 text-red-600"/>
                <h1 className="text-3xl font-bold">YouTube Аккаунты</h1>
            </div>

            <div className="grid gap-6 lg:grid-cols-[1fr,400px]">
                <Card>
                    <CardHeader>
                        <CardTitle>Мои аккаунты</CardTitle>
                        <CardDescription>
                            Управление подключенными YouTube аккаунтами
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        {isLoading ? (
                            <div className="flex items-center justify-center py-8">
                                <Loader2 className="h-8 w-8 animate-spin text-muted-foreground"/>
                            </div>
                        ) : !accounts || accounts.length === 0 ? (
                            <div className="text-center py-8 text-muted-foreground">
                                <Video className="h-12 w-12 mx-auto mb-4 opacity-50"/>
                                <p>У вас пока нет подключенных аккаунтов</p>
                                <p className="text-sm mt-2">Добавьте первый аккаунт, используя форму справа</p>
                            </div>
                        ) : (
                            <div className="space-y-4">
                                {accounts.map((account) => (
                                    <div
                                        key={account.id}
                                        className="flex items-start justify-between p-4 border rounded-lg hover:bg-accent/50 transition-colors"
                                    >
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-center gap-2 mb-2">
                                                <Video className="h-5 w-5 text-red-600 flex-shrink-0"/>
                                                <h3 className="font-semibold truncate">{account.name}</h3>
                                            </div>
                                            {account.defaultTitle && (
                                                <p className="text-sm text-muted-foreground mb-1">
                                                    <span
                                                        className="font-medium">Заголовок по умолчанию:</span> {account.defaultTitle}
                                                </p>
                                            )}
                                            {account.defaultDescription && (
                                                <p className="text-sm text-muted-foreground mb-1">
                                                    <span
                                                        className="font-medium">Описание:</span> {account.defaultDescription}
                                                </p>
                                            )}
                                            {account.defaultTags && (
                                                <p className="text-sm text-muted-foreground mb-1">
                                                    <span className="font-medium">Теги:</span> {account.defaultTags}
                                                </p>
                                            )}
                                            {account.autoPostingVideo && (
                                                <p className="text-sm text-green-600 font-medium mt-2">
                                                    ✓ Автопостинг включен
                                                </p>
                                            )}
                                        </div>
                                        <AlertDialog>
                                            <AlertDialogTrigger asChild>
                                                <Button
                                                    variant="ghost"
                                                    size="icon"
                                                    className="text-destructive hover:text-destructive hover:bg-destructive/10 flex-shrink-0 ml-4"
                                                >
                                                    <Trash2 className="h-4 w-4"/>
                                                </Button>
                                            </AlertDialogTrigger>
                                            <AlertDialogContent>
                                                <AlertDialogHeader>
                                                    <AlertDialogTitle>Удалить аккаунт?</AlertDialogTitle>
                                                    <AlertDialogDescription>
                                                        Вы уверены, что хотите удалить аккаунт "{account.name}"?
                                                        Это действие нельзя отменить.
                                                    </AlertDialogDescription>
                                                </AlertDialogHeader>
                                                <AlertDialogFooter>
                                                    <AlertDialogCancel>Отмена</AlertDialogCancel>
                                                    <AlertDialogAction
                                                        onClick={() => handleDelete(account.id)}
                                                        className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                                                    >
                                                        Удалить
                                                    </AlertDialogAction>
                                                </AlertDialogFooter>
                                            </AlertDialogContent>
                                        </AlertDialog>
                                    </div>
                                ))}
                            </div>
                        )}
                    </CardContent>
                </Card>

                {/* Правая колонка с формой и инструкцией */}
                <div className="space-y-6">
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
                                                render={({field: {onChange, value, ...field}}) => (
                                                    <FormItem>
                                                        <FormLabel>Файл client_secret.json</FormLabel>
                                                        <FormControl>
                                                            <div className="grid w-full items-center gap-1.5">
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
                                                        <FormMessage/>
                                                    </FormItem>
                                                )}
                                            />
                                            <Button type="submit" disabled={isPending} className="w-full">
                                                {isPending ? (
                                                    <>
                                                        <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
                                                        Перенаправление...
                                                    </>
                                                ) : (
                                                    <>
                                                        <Upload className="mr-2 h-4 w-4"/>
                                                        Авторизоваться
                                                    </>
                                                )}
                                            </Button>
                                        </form>
                                    </Form>
                                </TabsContent>

                                <TabsContent value="manual">
                                    <Form {...manualForm}>
                                        <form onSubmit={manualForm.handleSubmit(onManualSubmit)}
                                              className="space-y-4 pt-4">
                                            <FormField
                                                control={manualForm.control}
                                                name="clientId"
                                                render={({field}) => (
                                                    <FormItem>
                                                        <FormLabel>Client ID</FormLabel>
                                                        <FormControl>
                                                            <Input placeholder="Ваш Client ID" {...field} />
                                                        </FormControl>
                                                        <FormMessage/>
                                                    </FormItem>
                                                )}
                                            />
                                            <FormField
                                                control={manualForm.control}
                                                name="clientSecret"
                                                render={({field}) => (
                                                    <FormItem>
                                                        <FormLabel>Client Secret</FormLabel>
                                                        <FormControl>
                                                            <Input type="password"
                                                                   placeholder="Ваш Client Secret" {...field} />
                                                        </FormControl>
                                                        <FormMessage/>
                                                    </FormItem>
                                                )}
                                            />
                                            <Button type="submit" disabled={isPending} className="w-full">
                                                {isPending ? (
                                                    <>
                                                        <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
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
                                <li>Перейдите в <a href="https://console.cloud.google.com/" target="_blank"
                                                   rel="noreferrer" className="text-blue-600 hover:underline">Google
                                    Cloud Console</a>.
                                </li>
                                <li>Создайте новый проект или выберите существующий.</li>
                                <li>В меню выберите "APIs & Services" → "Library" и включите "YouTube Data API v3".</li>
                                <li>Перейдите в "APIs & Services" → "Credentials".</li>
                                <li>Нажмите "Create Credentials" → "OAuth client ID".</li>
                                <li>Выберите тип приложения "Web application".</li>
                                <li>В "Authorized redirect URIs" добавьте: <code
                                    className="bg-muted px-1 py-0.5 rounded">{window.location.origin}/api/v1/youtube/callback</code>
                                </li>
                                <li>Нажмите "Create" и скачайте JSON файл или скопируйте Client ID и Secret.</li>
                            </ol>
                        </CardContent>
                    </Card>
                </div>
            </div>
        </div>
    );
}
