# CRM

## OTP and password change

Before deploying the OTP feature, apply `Backend/CRM.Api/Data/add_user_otp_columns.sql` to the PostgreSQL database. The API encrypts authenticator secrets with ASP.NET Core Data Protection. In production, configure `DataProtection__KeysPath` to a persistent, access-controlled directory (shared by API instances); losing the key ring makes saved OTP secrets unreadable.

Users can set up OTP in **Dashboard → Hồ sơ** with an authenticator app. Password changes require the account email, a current six-digit authenticator code, and a new password of at least 12 characters.