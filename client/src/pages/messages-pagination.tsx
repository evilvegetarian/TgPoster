import {
    Pagination,
    PaginationContent,
    PaginationEllipsis,
    PaginationItem,
    PaginationLink,
    PaginationNext,
    PaginationPrevious
} from "@/components/ui/pagination";


interface Props {
    currentPage: number;
    totalPages: number;
    onPageChange: (page: number) => void;
}

export function MessagesPagination({currentPage, totalPages, onPageChange}: Props) {
    if (totalPages <= 1) return null;

    const maxVisible = 2;
    const items = [];

    // Кнопка "Назад"
    items.push(
        <PaginationItem key="prev">
            <PaginationPrevious
                onClick={() => onPageChange(Math.max(1, currentPage - 1))}
                className={currentPage === 1 ? "pointer-events-none opacity-50" : "cursor-pointer"}
            />
        </PaginationItem>
    );

    // Первая страница
    items.push(
        <PaginationItem key={1}>
            <PaginationLink
                onClick={() => onPageChange(1)}
                isActive={currentPage === 1}
                className="cursor-pointer"
            >
                1
            </PaginationLink>
        </PaginationItem>
    );

    const startPage = Math.max(2, currentPage - maxVisible);
    const endPage = Math.min(totalPages - 1, currentPage + maxVisible);

    if (startPage > 2) {
        items.push(<PaginationItem key="ellipsis-start"><PaginationEllipsis/></PaginationItem>);
    }

    for (let i = startPage; i <= endPage; i++) {
        items.push(
            <PaginationItem key={i}>
                <PaginationLink
                    onClick={() => onPageChange(i)}
                    isActive={currentPage === i}
                    className="cursor-pointer"
                >
                    {i}
                </PaginationLink>
            </PaginationItem>
        );
    }

    if (endPage < totalPages - 1) {
        items.push(<PaginationItem key="ellipsis-end"><PaginationEllipsis/></PaginationItem>);
    }

    // Последняя страница
    if (totalPages > 1) {
        items.push(
            <PaginationItem key={totalPages}>
                <PaginationLink
                    onClick={() => onPageChange(totalPages)}
                    isActive={currentPage === totalPages}
                    className="cursor-pointer"
                >
                    {totalPages}
                </PaginationLink>
            </PaginationItem>
        );
    }

    // Кнопка "Вперед"
    items.push(
        <PaginationItem key="next">
            <PaginationNext
                onClick={() => onPageChange(Math.min(totalPages, currentPage + 1))}
                className={currentPage === totalPages ? "pointer-events-none opacity-50" : "cursor-pointer"}
            />
        </PaginationItem>
    );

    return (
        <Pagination>
            <PaginationContent>{items}</PaginationContent>
        </Pagination>
    );
}


