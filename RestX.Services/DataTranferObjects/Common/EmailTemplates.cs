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
        public static string ReservationConfirmation(
            string name,
            string confirmationCode,
            DateTime reservationDateTime,
            int numberOfGuests,
            string tableList,
            string? specialRequests,
            decimal? depositAmount = null,
            DateTime? paymentDeadline = null,
            string? paymentLink = null)
        {
            var specialRequestsSection = !string.IsNullOrWhiteSpace(specialRequests)
                ? $"<p><strong>Special Requests:</strong> {specialRequests}</p>"
                : string.Empty;

            var depositSection = (depositAmount.HasValue && paymentDeadline.HasValue && !string.IsNullOrWhiteSpace(paymentLink))
                ? $@"
                        <hr style='border:none;border-top:1px solid #ddd;margin:12px 0' />
                        <p style='color:#d9534f;margin-top:12px'><strong>⚠️ Deposit Payment Required</strong></p>
                        <p><strong>Amount:</strong> {depositAmount:N0} VND</p>
                        <p><strong>Deadline:</strong> {paymentDeadline:dd/MM/yyyy HH:mm}</p>
                        <p style='margin-top:12px'>
                            <a href='{paymentLink}'
                               style='display:inline-block;padding:10px 20px;
                               background:#d9534f;color:#fff;text-decoration:none;
                               border-radius:4px;font-weight:bold'>
                                Pay Deposit Now
                            </a>
                        </p>"
                : string.Empty;

            return $@"
            <html>
            <body style='font-family: Arial, sans-serif; background:#f9f9f9; padding:20px'>
                <div style='max-width:600px;margin:auto;background:#fff;padding:20px;border-radius:6px'>
                    <h2 style='color:#333'>Reservation Confirmed!</h2>
                    <p>Hi <strong>{name}</strong>,</p>
                    <p>Your reservation has been received. Here are the details:</p>
                    <div style='background:#f0f4ff;padding:16px;border-radius:6px;margin:16px 0'>
                        <p><strong>Confirmation Code:</strong> <span style='font-size:1.2em;color:#007bff;letter-spacing:2px'>{confirmationCode}</span></p>
                        <p><strong>Date &amp; Time:</strong> {reservationDateTime:dddd, dd MMMM yyyy} at {reservationDateTime:HH:mm}</p>
                        <p><strong>Number of Guests:</strong> {numberOfGuests}</p>
                        <p><strong>Table(s):</strong> {tableList}</p>
                        {specialRequestsSection}{depositSection}
                    </div>
                    <p>Please keep your confirmation code — you will need it to manage or look up your reservation.</p>
                    <p>If you need to cancel or make changes, please contact us with your confirmation code and phone number.</p>
                    <hr />
                    <small>This is an automated email. Please do not reply.</small>
                </div>
            </body>
            </html>";
        }
    }
}
