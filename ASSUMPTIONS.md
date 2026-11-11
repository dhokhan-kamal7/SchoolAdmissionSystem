# Assumptions

1. A student's identity is represented by a 17-digit numeric birth registration number.
2. Students do not need an account or password. The frontend sends the student ID through the X-Student-Id header.
3. Payment is simulated through the confirmation endpoint; no real payment gateway is integrated.
4. A seat is reserved temporarily when an application is created.
5. The default hold duration is 600 seconds and can be configured using HOLD_DURATION_SECONDS.
6. Expired holds are automatically marked as EXPIRED by a background service.
7. Confirmed applications permanently occupy a seat unless additional administrative functionality is introduced.
8. PostgreSQL is used for persistent application and class data.
9. Class shifts are optional.
