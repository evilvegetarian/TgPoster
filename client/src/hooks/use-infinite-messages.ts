import {useInfiniteQuery} from "@tanstack/react-query"
import {getApiV1Message} from "@/api/endpoints/message/message"
import type {
    GetApiV1MessageParams,
    MessageResponsePagedResponse
} from "@/api/endpoints/tgPosterAPI.schemas"

type InfiniteMessagesParams = Omit<GetApiV1MessageParams, "PageNumber">

export function useInfiniteMessages(params: InfiniteMessagesParams) {
    return useInfiniteQuery<MessageResponsePagedResponse>({
        queryKey: ["/api/v1/message", params, "infinite"],
        queryFn: ({pageParam, signal}) =>
            getApiV1Message(
                {...params, PageNumber: pageParam as number},
                signal
            ),
        initialPageParam: 1,
        getNextPageParam: (lastPage) =>
            lastPage.hasNextPage ? (lastPage.currentPage ?? 0) + 1 : undefined,
        enabled: !!params.ScheduleId,
    })
}
