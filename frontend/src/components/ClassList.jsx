import { useEffect, useState } from "react";
import { getClasses } from "../services/api";

function ClassList({ onSelectClass }) {
    const [classes, setClasses] = useState([]);

    useEffect(() => {
        getClasses()
            .then(setClasses)
            .catch(console.error);
    }, []);

    return (
        <div>
            <h2>Available Classes</h2>

            {classes.map((schoolClass) => (
                <div key={schoolClass.id}>
                    <h3>{schoolClass.name}</h3>
                    <p>Shift: {schoolClass.shift}</p>
                    <p>Total Seats: {schoolClass.totalSeats}</p>
                    <button onClick={() => onSelectClass(schoolClass)}>
                        Apply
                    </button>
                </div>
            ))}
        </div>
    );
}

export default ClassList;