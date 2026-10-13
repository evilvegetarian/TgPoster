import {useCallback, useState} from "react";
import {useGetApiV1ChannelCleanupJobsJobId} from "@/api/endpoints/channel-cleanup/channel-cleanup.ts";
import {ChannelCleanupStatus} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import type {ChannelCleanupJobResponse} from "@/api/endpoints/tgPosterAPI.schemas.ts";

const STORAGE_KEY_PREFIX = "tgposter.channel-cleanup-job.";
const POLL_INTERVAL_MS = 3000;

const ACTIVE_STATUSES: ChannelCleanupStatus[] = [
    ChannelCleanupStatus.Pending,
    ChannelCleanupStatus.InProgress,
    ChannelCleanupStatus.CooldownWait,
];

export function isCleanupJobActive(job: ChannelCleanupJobResponse | undefined): boolean {
    return job != null && ACTIVE_STATUSES.includes(job.status);
}

function readStoredJobId(sessionId: string): string | null {
    try {
        return localStorage.getItem(STORAGE_KEY_PREFIX + sessionId);
    } catch {
        return null;
    }
}

function writeStoredJobId(sessionId: string, jobId: string | null) {
    try {
        if (jobId === null) {
            localStorage.removeItem(STORAGE_KEY_PREFIX + sessionId);
        } else {
            localStorage.setItem(STORAGE_KEY_PREFIX + sessionId, jobId);
        }
    } catch {
        // Приватный режим или запрет на хранение: задача продолжится на сервере, прогресс просто не переживёт перезагрузку
    }
}

/**
 * Отслеживает очистку каналов конкретного аккаунта.
 * Id задачи хранится в localStorage отдельно для каждой сессии, поэтому прогресс переживает перезагрузку
 */
export function useChannelCleanupJob(sessionId: string) {
    const [jobId, setJobId] = useState<string | null>(() => readStoredJobId(sessionId));

    const {data: job, isLoading, error} = useGetApiV1ChannelCleanupJobsJobId(jobId ?? "", {
        query: {
            enabled: jobId !== null,
            retry: false,
            refetchInterval: (query) => isCleanupJobActive(query.state.data) ? POLL_INTERVAL_MS : false,
        },
    });

    const startJob = useCallback((newJobId: string) => {
        writeStoredJobId(sessionId, newJobId);
        setJobId(newJobId);
    }, [sessionId]);

    const clearJob = useCallback(() => {
        writeStoredJobId(sessionId, null);
        setJobId(null);
    }, [sessionId]);

    return {
        jobId,
        job,
        // Задачу удалили или она чужая — забываем её, иначе диалог навсегда застрянет на загрузке
        isMissing: jobId !== null && error != null,
        isLoading: isLoading && jobId !== null,
        isActive: isCleanupJobActive(job),
        startJob,
        clearJob,
    };
}
