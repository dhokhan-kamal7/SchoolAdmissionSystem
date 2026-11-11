
import { useEffect, useState } from "react";
import {
    getClasses,
    createClass,
    createHold,
    getMyHolds,
    confirmHold,
    cancelHold
} from "./services/api";

function App() {
    const [studentId, setStudentId] = useState(
        () => localStorage.getItem("studentId") || ""
    );
    const [classes, setClasses] = useState([]);
    const [holds, setHolds] = useState([]);
    const [page, setPage] = useState("classes");
    const [message, setMessage] = useState("");
    const [loading, setLoading] = useState(false);

    function updateStudentId(value) {
        setStudentId(value);
        localStorage.setItem("studentId", value);
    }

    async function loadClasses() {
        try {
            setClasses(await getClasses());
        } catch (error) {
            setMessage(error.message);
        }
    }

    async function loadHolds() {
        if (!/^\d{17}$/.test(studentId)) return;

        try {
            setHolds(await getMyHolds(studentId));
        } catch (error) {
            setMessage(error.message);
        }
    }

    useEffect(() => {
        loadClasses();
        const timer = setInterval(loadClasses, 5000);
        return () => clearInterval(timer);
    }, []);

    useEffect(() => {
        if (page !== "applications" || !/^\d{17}$/.test(studentId)) return;

        loadHolds();
        const timer = setInterval(loadHolds, 5000);
        return () => clearInterval(timer);
    }, [page, studentId]);

    async function apply(classId) {
        if (!/^\d{17}$/.test(studentId)) {
            setMessage("Enter a valid 17-digit birth registration number first.");
            return;
        }

        setLoading(true);
        setMessage("");

        try {
            await createHold(classId, studentId);
            setMessage("Application created successfully. Your seat is held.");
            await loadClasses();
            await loadHolds();
            setPage("applications");
        } catch (error) {
            setMessage(error.message);
        } finally {
            setLoading(false);
        }
    }

    async function pay(holdId) {
        setMessage("");

        try {
            await confirmHold(holdId, studentId);
            setMessage("Application confirmed successfully!");
            await loadHolds();
            await loadClasses();
        } catch (error) {
            setMessage(error.message);
            await loadHolds();
        }
    }

    async function cancel(holdId) {
        setMessage("");

        try {
            await cancelHold(holdId, studentId);
            setMessage("Application cancelled.");
            await loadHolds();
            await loadClasses();
        } catch (error) {
            setMessage(error.message);
        }
    }

    return (
        <main style={{ maxWidth: "900px", margin: "30px auto", padding: "20px" }}>
            <h1>School Admission System</h1>

            <label>Student Birth Registration Number (17 digits)</label>
            <br />
            <input
                type="text"
                value={studentId}
                maxLength={17}
                placeholder="Enter 17-digit number"
                onChange={(e) => updateStudentId(e.target.value.replace(/\D/g, ""))}
                style={{ padding: "10px", width: "280px", margin: "10px 0" }}
            />

            <p>
                <button onClick={() => { setPage("classes"); setMessage(""); }}>
                    Available Classes
                </button>{" "}
                <button onClick={() => { setPage("applications"); setMessage(""); }}>
                    My Applications
                </button>{" "}
                <button onClick={() => { setPage("admin"); setMessage(""); }}>
                    Admin Panel
                </button>
            </p>

            {message && (
                <p style={{ padding: "10px", background: "#f0f0f0" }}>
                    {message}
                </p>
            )}

            {page === "classes" && (
                <>
                    <h2>Available Classes</h2>

                    {classes.map((schoolClass) => (
                        <div key={schoolClass.id}
                            style={{ border: "1px solid #ccc", padding: "15px", marginBottom: "12px" }}>
                            <h3>{schoolClass.name}</h3>
                            <p>Total Seats: {schoolClass.totalSeats}</p>
                            <p>Held Seats: {schoolClass.heldSeats}</p>
                            <p>Confirmed Seats: {schoolClass.confirmedSeats}</p>
                            <p>Available Seats: {schoolClass.availableSeats}</p>

                            <button
                                disabled={loading || schoolClass.availableSeats <= 0}
                                onClick={() => apply(schoolClass.id)}>
                                {schoolClass.availableSeats <= 0 ? "Full" : "Apply"}
                            </button>
                        </div>
                    ))}
                </>
            )}

            {page === "applications" && (
                <>
                    <h2>My Applications</h2>

                    {!/^\d{17}$/.test(studentId) && (
                        <p>Enter your valid 17-digit birth registration number above.</p>
                    )}

                    {holds.map((hold) => {
                        const expired =
                            hold.status === "EXPIRED" ||
                            (hold.status === "HELD" &&
                                new Date(hold.expiresAt) <= new Date());

                        const status = expired ? "EXPIRED" : hold.status;

                        return (
                            <div key={hold.holdId}
                                style={{ border: "1px solid #ccc", padding: "15px", marginBottom: "12px" }}>
                                <h3>{hold.className}</h3>
                                <p>Status: {status}</p>
                                <p>Expires: {new Date(hold.expiresAt).toLocaleString()}</p>

                                {status === "HELD" && (
                                    <>
                                        <button onClick={() => pay(hold.holdId)}>
                                            Simulate Payment
                                        </button>{" "}
                                        <button onClick={() => cancel(hold.holdId)}>
                                            Cancel
                                        </button>
                                    </>
                                )}
                            </div>
                        );
                    })}

                    {holds.length === 0 && /^\d{17}$/.test(studentId) && (
                        <p>No applications found.</p>
                    )}
                </>
            )}

            {page === "admin" && <Admin />}
        </main>
    );
}

function Admin() {
    const [name, setName] = useState("");
    const [shift, setShift] = useState("");
    const [seats, setSeats] = useState("");
    const [message, setMessage] = useState("");

    async function submit(event) {
        event.preventDefault();
        setMessage("");

        try {
            await createClass(name, shift, Number(seats));
            setMessage("Class created successfully.");
            setName("");
            setShift("");
            setSeats("");
        } catch (error) {
            setMessage(error.message);
        }
    }

    return (
        <>
            <h2>Admin Panel</h2>
            <form onSubmit={submit}>
                <input
                    placeholder="Class Name"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    required
                />
                <br /><br />

                <input
                    placeholder="Shift"
                    value={shift}
                    onChange={(e) => setShift(e.target.value)}
                />
                <br /><br />

                <input
                    type="number"
                    placeholder="Total Seats"
                    min="1"
                    value={seats}
                    onChange={(e) => setSeats(e.target.value)}
                    required
                />
                <br /><br />

                <button type="submit">Create Class</button>
            </form>

            {message && <p>{message}</p>}
        </>
    );
}

export default App;
