 export function convertLocalToIsoTime(time: string) {

    const timeParts = time.split(':');
    if (timeParts.length < 2) {
        console.error(`Неверный формат времени: ${time}`);
        return time;
    }

    const hours = parseInt(timeParts[0], 10);
    const minutes = parseInt(timeParts[1], 10);
    const seconds = parseInt(timeParts[2] || '0', 10);

    if (isNaN(hours) || isNaN(minutes) || isNaN(seconds)) {
        console.error(`Неверные компоненты времени в: ${time}`);
        return time;
    }

    const date = new Date();
    date.setHours(hours, minutes, seconds, 0);
    return new Date(date).toLocaleTimeString('ru', {timeStyle: 'short', hour12: false, timeZone: 'UTC'});

}

 export function convertUtcTimeToLocal(utcTimeString: string): string {
    if (!utcTimeString || typeof utcTimeString !== 'string') {
        return 'Invalid Time';
    }

    const timeParts = utcTimeString.split(':');
    if (timeParts.length < 2) {
        console.error(`Неверный формат времени: ${utcTimeString}`);
        return utcTimeString;
    }

    const hours = parseInt(timeParts[0], 10);
    const minutes = parseInt(timeParts[1], 10);
    const seconds = parseInt(timeParts[2] || '0', 10);

    if (isNaN(hours) || isNaN(minutes) || isNaN(seconds)) {
        console.error(`Неверные компоненты времени в: ${utcTimeString}`);
        return utcTimeString;
    }

    const date = new Date();
    date.setUTCHours(hours, minutes, seconds, 0);

    return date.toLocaleTimeString();
}