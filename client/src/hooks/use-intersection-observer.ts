import {useEffect, useRef, useCallback} from "react"

interface UseIntersectionObserverOptions {
    enabled?: boolean
    rootMargin?: string
    threshold?: number
}

export function useIntersectionObserver(
    callback: () => void,
    options: UseIntersectionObserverOptions = {}
) {
    const {enabled = true, rootMargin = "200px", threshold = 0} = options
    const sentinelRef = useRef<HTMLDivElement | null>(null)
    const callbackRef = useRef(callback)

    callbackRef.current = callback

    const stableCallback = useCallback(() => {
        callbackRef.current()
    }, [])

    useEffect(() => {
        const el = sentinelRef.current
        if (!el || !enabled) return

        const observer = new IntersectionObserver(
            ([entry]) => {
                if (entry.isIntersecting) stableCallback()
            },
            {rootMargin, threshold}
        )

        observer.observe(el)
        return () => observer.disconnect()
    }, [enabled, rootMargin, threshold, stableCallback])

    return sentinelRef
}
