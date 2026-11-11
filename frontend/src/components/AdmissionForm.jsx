import { useState } from "react";
import { createHold } from "../services/api";

function AdmissionForm({ schoolClass, onSuccess }) {
    const [studentName, setStudentName] = useState("");
    const [parentEmail, setParentEmail] = useState("");
    const [error, setError] = useState("");

    async function handleSubmit(event) {
        event.preventDefault();
        setError("");

        try {
            const hold = await createHold(
                schoolClass.id,
                studentName,
                parentEmail
            );

            onSuccess(hold, parentEmail);
        } catch (err) {
            setError(err.message);
        }
    }

    return (
        <div>
            <h2>Apply for {schoolClass.name}</h2>

            <form onSubmit={handleSubmit}>
                <input
                    type="text"
                    placeholder="Student Name"
                    value={studentName}
                    onChange={(e) => setStudentName(e.target.value)}
                    required
                />

                <input
                    type="email"
                    placeholder="Parent Email"
                    value={parentEmail}
                    onChange={(e) => setParentEmail(e.target.value)}
                    required
                />

                <button type="submit">Apply Now</button>
            </form>

            {error && <p>{error}</p>}
        </div>
    );
}

export default AdmissionForm;