import {format} from "date-fns";
import {ru} from "date-fns/locale";

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

 export function convertUtcTimeToLocalTime(utcTimeString: string): string {
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

 export function utcToLocalString  (utcString: string): string {
     const date = new Date(utcString);
     return format(date, "yyyy-MM-dd'T'HH:mm");
 };

export function utcToShortLocalString  (utcString: string): string {
  return   format(new Date(utcString), "dd MMM HH:mm", {locale: ru})
};