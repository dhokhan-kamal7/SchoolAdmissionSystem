
export const API_URL = import.meta.env.VITE_API_URL || "http://localhost:8080";

async function request(url, options = {}) {
    const response = await fetch(`${API_URL}${url}`, options);

    if (response.status === 204) return null;

    const text = await response.text();
    let data;

    try {
        data = text ? JSON.parse(text) : null;
    } catch {
        data = text;
    }

    if (!response.ok) {
        throw new Error(
            typeof data === "string"
                ? data
                : data?.detail || data?.title || "Something went wrong."
        );
    }

    return data;
}

function studentHeaders(studentId) {
    return { "X-Student-Id": studentId };
}

export function getClasses() {
    return request("/classes");
}

export function createClass(name, shift, totalSeats) {
    return request("/classes", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name, shift, totalSeats })
    });
}

export function createHold(classId, studentId) {
    return request(`/classes/${classId}/holds`, {
        method: "POST",
        headers: studentHeaders(studentId)
    });
}

export function getMyHolds(studentId) {
    return request("/me/holds", {
        headers: studentHeaders(studentId)
    });
}

export function confirmHold(holdId, studentId) {
    return request(`/holds/${holdId}/confirm`, {
        method: "POST",
        headers: studentHeaders(studentId)
    });
}

export function cancelHold(holdId, studentId) {
    return request(`/holds/${holdId}`, {
        method: "DELETE",
        headers: studentHeaders(studentId)
    });
}
