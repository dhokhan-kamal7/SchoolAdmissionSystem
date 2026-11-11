import { useState } from "react";

function AdminPanel() {
    const [name, setName] = useState("");
    const [shift, setShift] = useState("");
    const [totalSeats, setTotalSeats] = useState("");
    const [message, setMessage] = useState("");

    async function handleSubmit(event) {
        event.preventDefault();
        setMessage("");

        try {
            const response = await fetch("http://localhost:8080/classes", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    name,
                    shift,
                    totalSeats: Number(totalSeats)
                })
            });

            if (!response.ok) {
                throw new Error("Unable to create class.");
            }

            setMessage("Class created successfully.");
            setName("");
            setShift("");
            setTotalSeats("");
        } catch (error) {
            setMessage(error.message);
        }
    }

    return (
        <div>
            <h2>Admin Panel</h2>

            <form onSubmit={handleSubmit}>
                <input
                    placeholder="Class Name"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    required
                />

                <input
                    placeholder="Shift"
                    value={shift}
                    onChange={(e) => setShift(e.target.value)}
                    required
                />

                <input
                    type="number"
                    placeholder="Total Seats"
                    min="1"
                    value={totalSeats}
                    onChange={(e) => setTotalSeats(e.target.value)}
                    required
                />

                <button type="submit">Create Class</button>
            </form>

            {message && <p>{message}</p>}
        </div>
    );
}

export default AdminPanel;