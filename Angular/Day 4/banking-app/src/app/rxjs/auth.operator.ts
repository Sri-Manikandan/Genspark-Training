import { Subject } from "rxjs";

export const usernameSubject = new Subject<string>();

export const changeUsername = (username: string) => {
    console.log("Changing username to", username);
    usernameSubject.next(username);
}

export function getUsernameFromToken(): string | null {
    const token = sessionStorage.getItem('token');
    if (!token) return null;
    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'] ?? null;
}