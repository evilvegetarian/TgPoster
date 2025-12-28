
import { useState, useCallback } from "react";

// Тип для функции обновления состояния
type SetValue<T> = (value: T | ((val: T) => T)) => void;
function usePersistentState<T>(
    key: string,
    initialValue: T
): [T, SetValue<T>] {
    // Получаем значение из localStorage или используем initialValue
    const [storedValue, setStoredValue] = useState<T>(() => {
        if (typeof window === "undefined") {
            return initialValue;
        }
        try {
            const item = window.localStorage.getItem(key);
            if (item === null) return initialValue;

            // Пытаемся распарсить JSON (для чисел/объектов), иначе возвращаем строку
            try {
                return JSON.parse(item);
            } catch {
                return item as unknown as T;
            }
        } catch (error) {
            console.error(`Error reading localStorage key "${key}":`, error);
            return initialValue;
        }
    });

    // Обертка над сеттером, которая сохраняет в localStorage
    const setValue: SetValue<T> = useCallback((value) => {
        try {
            // Разрешаем value быть функцией (как в стандартном useState)
            const valueToStore = value instanceof Function ? value(storedValue) : value;

            setStoredValue(valueToStore);

            if (typeof window !== "undefined") {
                if (valueToStore === "" || valueToStore === undefined || valueToStore === null) {
                    window.localStorage.removeItem(key);
                } else {
                    // Если строка — сохраняем как есть, иначе JSON.stringify
                    const data = typeof valueToStore === 'string'
                        ? valueToStore
                        : JSON.stringify(valueToStore);
                    window.localStorage.setItem(key, data);
                }
            }
        } catch (error) {
            console.error(`Error saving localStorage key "${key}":`, error);
        }
    }, [key, storedValue]);

    return [storedValue, setValue];
}

export default usePersistentState;