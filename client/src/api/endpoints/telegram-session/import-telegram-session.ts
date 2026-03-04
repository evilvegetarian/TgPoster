import { useMutation } from "@tanstack/react-query";
import type {
    UseMutationOptions,
    UseMutationResult,
    QueryClient,
} from "@tanstack/react-query";
import type { ProblemDetails } from "../tgPosterAPI.schemas";
import { customInstance } from "../../axios-instance";

export interface ImportTelegramSessionRequest {
    apiId: string;
    apiHash: string;
    sessionFile: File;
    name?: string | null;
}

export interface ImportTelegramSessionResponse {
    id: string;
    name: string;
    isActive: boolean;
    phoneNumber: string;
}

export const postApiV1TelegramSessionImport = (
    request: ImportTelegramSessionRequest,
    signal?: AbortSignal
) => {
    const formData = new FormData();
    formData.append("ApiId", request.apiId);
    formData.append("ApiHash", request.apiHash);
    formData.append("SessionFile", request.sessionFile);
    if (request.name) {
        formData.append("Name", request.name);
    }

    return customInstance<ImportTelegramSessionResponse>({
        url: `/api/v1/telegram-session/import`,
        method: "POST",
        headers: { "Content-Type": "multipart/form-data" },
        data: formData,
        signal,
    });
};

export const usePostApiV1TelegramSessionImport = <
    TError = ProblemDetails,
    TContext = unknown,
>(
    options?: {
        mutation?: UseMutationOptions<
            Awaited<ReturnType<typeof postApiV1TelegramSessionImport>>,
            TError,
            { data: ImportTelegramSessionRequest },
            TContext
        >;
    },
    queryClient?: QueryClient
): UseMutationResult<
    Awaited<ReturnType<typeof postApiV1TelegramSessionImport>>,
    TError,
    { data: ImportTelegramSessionRequest },
    TContext
> => {
    const { mutation: mutationOptions } = options ?? {};

    const mutationFn = (props: { data: ImportTelegramSessionRequest }) => {
        const { data } = props;
        return postApiV1TelegramSessionImport(data);
    };

    return useMutation(
        { mutationFn, mutationKey: ["postApiV1TelegramSessionImport"], ...mutationOptions },
        queryClient
    );
};
