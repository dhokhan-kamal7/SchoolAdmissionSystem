import { useEffect, useState } from "react";
import { cancelHold, confirmHold, getMyHolds } from "../services/api";

function MyHolds({ email }) {
    const [holds, setHolds] = useState([]);

    async function loadHolds() {
        const data = await getMyHolds(email);
        setHolds(data);
    }

    useEffect(() => {
        loadHolds();

        const interval = setInterval(loadHolds, 5000);

        return () => clearInterval(interval);
    }, [email]);

    async function handleConfirm(id) {
        await confirmHold(id);
        loadHolds();
    }

    async function handleCancel(id) {
        await cancelHold(id);
        loadHolds();
    }

    return (
        <div>
            <h2>My Applications</h2>

            {holds.map((hold) => (
                <div key={hold.id}>
                    <h3>{hold.schoolClass?.name}</h3>
                    <p>Status: {hold.status}</p>
                    <p>Expires: {new Date(hold.expiresAtUtc).toLocaleString()}</p>

                    {hold.status === "Active" && (
                        <>
                            <button onClick={() => handleConfirm(hold.id)}>
                                Simulate Payment
                            </button>

                            <button onClick={() => handleCancel(hold.id)}>
                                Cancel
                            </button>
                        </>
                    )}
                </div>
            ))}
        </div>
    );
}

export default MyHolds;