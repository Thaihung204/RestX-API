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
      string? paymentLink = null,
      string? hostname = null,
      Guid? reservationId = null)
        {
            var encodedName = System.Net.WebUtility.HtmlEncode(name);
            var encodedTables = System.Net.WebUtility.HtmlEncode(tableList);
            var encodedRequests = System.Net.WebUtility.HtmlEncode(specialRequests ?? "");

            var specialRequestsSection = !string.IsNullOrWhiteSpace(specialRequests)
                ? $@"
                    <table width='100%' cellpadding='14' cellspacing='0' border='0'
                           style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:10px;margin-bottom:16px'>
                      <tr>
                        <td>
                          <span style='font-size:13px;color:#6b7280'>Special Requests</span><br/>
                          <span style='font-size:15px;color:#111827'>{encodedRequests}</span>
                        </td>
                      </tr>
                    </table>"
                : "";

            var viewDetailSection = (!string.IsNullOrWhiteSpace(hostname) && reservationId.HasValue)
                ? $@"
                    <table width='100%' cellpadding='0' cellspacing='0' border='0'
                           style='margin-bottom:16px'>
                      <tr>
                        <td style='text-align:center;padding:8px 0'>
                          <a href='https://{hostname}/your-reservation/{reservationId}'
                             style='display:inline-block;background:#2563eb;color:#ffffff;
                                    text-decoration:none;padding:13px 32px;
                                    border-radius:8px;font-size:15px;font-weight:600'>
                            View Reservation Details
                          </a>
                        </td>
                      </tr>
                    </table>"
                : "";

            var depositSection = (depositAmount.HasValue && depositAmount > 0
                                && paymentDeadline.HasValue
                                && !string.IsNullOrWhiteSpace(paymentLink))
                ? $@"
                    <table width='100%' cellpadding='16' cellspacing='0' border='0'
                           style='background:#fff5f5;border:1px solid #fecaca;
                                  border-radius:10px;margin-top:8px'>
                      <tr>
                        <td>
                          <div style='font-size:15px;font-weight:700;color:#b91c1c;margin-bottom:10px'>
                            &#9888; Deposit Required
                          </div>
                          <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                            <tr>
                              <td style='font-size:14px;color:#374151;padding-bottom:4px'>
                                <strong>Amount:</strong> {depositAmount:N0} VND
                              </td>
                            </tr>
                            <tr>
                              <td style='font-size:14px;color:#374151;padding-bottom:12px'>
                                <strong>Deadline:</strong> {paymentDeadline:dd/MM/yyyy HH:mm}
                              </td>
                            </tr>
                          </table>
                          <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                            <tr>
                              <td style='text-align:center'>
                                <a href='{paymentLink}'
                                   style='display:inline-block;background:#dc2626;color:#ffffff;
                                          text-decoration:none;padding:12px 28px;
                                          border-radius:8px;font-size:15px;font-weight:600'>
                                  Pay Now
                                </a>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>"
                : "";

            return $@"
                    <html>
                    <body style='margin:0;padding:0;background:#f3f4f6;font-family:Arial,sans-serif'>
                    <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background:#f3f4f6;padding:40px 0'>
                      <tr>
                        <td align='center'>
                          <table width='600' cellpadding='0' cellspacing='0' border='0'
                                 style='background:#ffffff;border-radius:12px;overflow:hidden;
                                        box-shadow:0 6px 24px rgba(0,0,0,0.06)'>

                            <!-- HEADER -->
                            <tr>
                              <td style='background:#111827;padding:28px;text-align:center'>
                                <h1 style='margin:0;font-size:24px;color:#ffffff;font-weight:700'>
                                  &#127881; Reservation Confirmed
                                </h1>
                              </td>
                            </tr>

                            <!-- BODY -->
                            <tr>
                              <td style='padding:32px'>

                                <p style='font-size:16px;margin:0 0 24px 0;color:#111827'>
                                  Hi <strong>{encodedName}</strong>,<br/>
                                  Your reservation has been confirmed. Here are your details:
                                </p>

                                <!-- CONFIRMATION CODE -->
                                <table width='100%' cellpadding='0' cellspacing='0' border='0'
                                       style='background:#eff6ff;border-radius:10px;margin-bottom:24px'>
                                  <tr>
                                    <td style='padding:20px;text-align:center'>
                                      <div style='font-size:12px;color:#6b7280;letter-spacing:1px;margin-bottom:8px'>
                                        CONFIRMATION CODE
                                      </div>
                                      <div style='font-size:32px;font-weight:700;letter-spacing:6px;color:#2563eb'>
                                        {confirmationCode}
                                      </div>
                                    </td>
                                  </tr>
                                </table>

                                <!-- DATE / TIME / GUESTS -->
                                <table width='100%' cellpadding='0' cellspacing='0' border='0'
                                       style='margin-bottom:20px'>
                                  <tr>
                                    <td width='33%' style='padding-right:8px'>
                                      <table width='100%' cellpadding='12' cellspacing='0' border='0'
                                             style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:10px'>
                                        <tr>
                                          <td style='text-align:center'>
                                            <div style='font-size:11px;color:#6b7280;letter-spacing:1px;margin-bottom:4px'>DATE</div>
                                            <div style='font-size:15px;font-weight:700;color:#111827'>{reservationDateTime:dd MMM yyyy}</div>
                                          </td>
                                        </tr>
                                      </table>
                                    </td>
                                    <td width='33%' style='padding:0 4px'>
                                      <table width='100%' cellpadding='12' cellspacing='0' border='0'
                                             style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:10px'>
                                        <tr>
                                          <td style='text-align:center'>
                                            <div style='font-size:11px;color:#6b7280;letter-spacing:1px;margin-bottom:4px'>TIME</div>
                                            <div style='font-size:15px;font-weight:700;color:#111827'>{reservationDateTime:HH:mm}</div>
                                          </td>
                                        </tr>
                                      </table>
                                    </td>
                                    <td width='33%' style='padding-left:8px'>
                                      <table width='100%' cellpadding='12' cellspacing='0' border='0'
                                             style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:10px'>
                                        <tr>
                                          <td style='text-align:center'>
                                            <div style='font-size:11px;color:#6b7280;letter-spacing:1px;margin-bottom:4px'>GUESTS</div>
                                            <div style='font-size:15px;font-weight:700;color:#111827'>{numberOfGuests}</div>
                                          </td>
                                        </tr>
                                      </table>
                                    </td>
                                  </tr>
                                </table>

                                <!-- TABLES -->
                                <table width='100%' cellpadding='14' cellspacing='0' border='0'
                                       style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:10px;margin-bottom:16px'>
                                  <tr>
                                    <td>
                                      <span style='font-size:13px;color:#6b7280'>Tables</span><br/>
                                      <span style='font-size:15px;font-weight:600;color:#111827'>{encodedTables}</span>
                                    </td>
                                  </tr>
                                </table>

                                {specialRequestsSection}

                                {viewDetailSection}

                                {depositSection}

                                <!-- NOTE -->
                                <p style='margin-top:24px;font-size:13px;color:#9ca3af;text-align:center'>
                                  Show your confirmation code when arriving.
                                </p>

                              </td>
                            </tr>

                            <!-- FOOTER -->
                            <tr>
                              <td style='background:#f9fafb;padding:16px;text-align:center;
                                         font-size:12px;color:#9ca3af;border-top:1px solid #e5e7eb'>
                                This is an automated email. Please do not reply.
                              </td>
                            </tr>

                          </table>
                        </td>
                      </tr>
                    </table>
                    </body>
                    </html>";
        }
    }
}
