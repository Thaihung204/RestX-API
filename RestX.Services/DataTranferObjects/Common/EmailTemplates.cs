namespace RestX.BLL.DataTranferObjects.Common
{
    public static class EmailTemplates
    {
        public static string PasswordReset(string resetLink)
        {
            return $@"
            <html>
            <body style='font-family: Arial, sans-serif; background:#f9f9f9; padding:20px'>
                <div style='max-width:600px;margin:auto;background:#fff;padding:20px;border-radius:6px'>
                    <h2>Password Reset</h2>
                    <p>You requested to reset your password.</p>
                    <p>
                        <a href='{resetLink}'
                           style='display:inline-block;padding:12px 24px;
                           background:#007bff;color:#fff;text-decoration:none;
                           border-radius:4px;font-weight:bold'>
                            Reset Password
                        </a>
                    </p>
                    <p>This link expires in <strong>15 minutes</strong>.</p>
                    <p>If you did not request this, please ignore this email.</p>
                    <hr />
                    <small>This is an automated email. Please do not reply.</small>
                </div>
            </body>
            </html>";
        }
        public static string WelcomeEmployee(string employeeName, string setPasswordLink)
        {
            return $@"
            <html>
            <body style='font-family: Arial, sans-serif; background:#f9f9f9; padding:20px'>
                <div style='max-width:600px;margin:auto;background:#fff;padding:20px;border-radius:6px'>
                    <h2>Welcome to RestX!</h2>
                    <p>Hi <strong>{employeeName}</strong>,</p>
                    <p>Your account has been created successfully. Please click the button below to set your password and complete your account setup.</p>
                    <p>
                        <a href='{setPasswordLink}'
                           style='display:inline-block;padding:12px 24px;
                           background:#28a745;color:#fff;text-decoration:none;
                           border-radius:4px;font-weight:bold'>
                            Set Your Password
                        </a>
                    </p>
                    <p>This link expires in <strong>24 hours</strong>.</p>
                    <p>If you did not expect this email, please contact your administrator.</p>
                    <hr />
                    <small>This is an automated email. Please do not reply.</small>
                </div>
            </body>
            </html>";
        }
    }
}
