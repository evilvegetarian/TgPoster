import {useCallback, useState} from "react";
import {useGetApiV1RepostImportJobsJobId} from "@/api/endpoints/repost/repost.ts";
import {RepostImportStatus} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import type {RepostImportJobResponse} from "@/api/endpoints/tgPosterAPI.schemas.ts";

const STORAGE_KEY = "tgposter.repost-import-job";
const POLL_INTERVAL_MS = 3000;

const ACTIVE_STATUSES: RepostImportStatus[] = [
    RepostImportStatus.Pending,
    RepostImportStatus.InProgress,
    RepostImportStatus.CooldownWait,
];

export function isJobActive(job: RepostImportJobResponse | undefined): boolean {
    return job != null && ACTIVE_STATUSES.includes(job.status);
}

function readStoredJobId(): string | null {
    try {
        return localStorage.getItem(STORAGE_KEY);
    } catch {
        return null;
    }
}

function writeStoredJobId(jobId: string | null) {
    try {
        if (jobId === null) {
            localStorage.removeItem(STORAGE_KEY);
        } else {
            localStorage.setItem(STORAGE_KEY, jobId);
        }
    } catch {
        // Приватный режим или запрет на хранение: задача продолжится, просто не переживёт перезагрузку
    }
}

/**
 * Отслеживает задачу массового добавления каналов в репост.
 * Id задачи лежит в localStorage, поэтому прогресс переживает перезагрузку страницы,
 * а сама задача продолжает работать на сервере даже с закрытой вкладкой.
 */
export function useRepostImportJob() {
    const [jobId, setJobId] = useState<string | null>(readStoredJobId);

    const {data: job, isLoading} = useGetApiV1RepostImportJobsJobId(jobId ?? "", {
        query: {
            enabled: jobId !== null,
            refetchInterval: (query) => isJobActive(query.state.data) ? POLL_INTERVAL_MS : false,
        },
    });

    const startJob = useCallback((newJobId: string) => {
        writeStoredJobId(newJobId);
        setJobId(newJobId);
    }, []);

    const clearJob = useCallback(() => {
        writeStoredJobId(null);
        setJobId(null);
    }, []);

    return {
        jobId,
        job,
        isLoading: isLoading && jobId !== null,
        isActive: isJobActive(job),
        startJob,
        clearJob,
    };
}
