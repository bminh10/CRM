# CRM

## OTP and password change

Before deploying the OTP feature, apply `Backend/CRM.Api/Data/add_user_otp_columns.sql` to the PostgreSQL database. The API encrypts authenticator secrets with ASP.NET Core Data Protection. In production, configure `DataProtection__KeysPath` to a persistent, access-controlled directory (shared by API instances); losing the key ring makes saved OTP secrets unreadable.

For the local `crm-local-postgres` container, run this once from the repository root in PowerShell:

```powershell
Get-Content -Raw .\Backend\CRM.Api\Data\add_user_otp_columns.sql | docker exec -i crm-local-postgres psql -X -v ON_ERROR_STOP=1 -U crm_local -d crm_local
```

The script adds nullable columns without changing existing user data. It fails if any of the columns already exist; inspect the schema before applying it to another database. Do not run it against a shared or production database without approval and a backup.

Users can set up OTP in **Dashboard → Hồ sơ** with an authenticator app. Password changes require the account email, a current six-digit authenticator code, and a new password of at least 12 characters.