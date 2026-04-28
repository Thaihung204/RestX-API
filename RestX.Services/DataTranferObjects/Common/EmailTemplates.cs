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
      Guid? reservationId = null,
      bool depositPaid = false,
      string? restaurantName = null,
      string? restaurantAddress = null,
      string? restaurantPhone = null)
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

            string depositSection = "";

            if (depositPaid && depositAmount.HasValue && depositAmount > 0)
            {
                depositSection = $@"
                    <table width='100%' cellpadding='16' cellspacing='0' border='0'
                           style='background:#f0fdf4;border:1px solid #bbf7d0;
                                  border-radius:10px;margin-top:8px'>
                      <tr>
                        <td>
                          <div style='font-size:15px;font-weight:700;color:#15803d;margin-bottom:6px'>
                            &#9989; Deposit Received
                          </div>
                          <div style='font-size:14px;color:#374151'>
                            We have received your deposit of <strong>{depositAmount:N0} VND</strong>.
                            Your reservation is now confirmed.
                          </div>
                        </td>
                      </tr>
                    </table>";
            }
            // Trường hợp chưa thanh toán — email gửi lúc tạo reservation
            else if (depositAmount.HasValue && depositAmount > 0
                     && paymentDeadline.HasValue
                     && !string.IsNullOrWhiteSpace(hostname)
                     && reservationId.HasValue)
            {
                depositSection = $@"
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
                                <a href='https://{hostname}/your-reservation/{reservationId}'
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
                    </table>";
            }

            var restaurantInfoSection = !string.IsNullOrWhiteSpace(restaurantName)
                ? $@"
                    <table width='100%' cellpadding='16' cellspacing='0' border='0'
                           style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:10px;margin-top:24px'>
                      <tr>
                        <td>
                          <div style='font-size:13px;font-weight:700;color:#374151;margin-bottom:6px'>
                            &#127860; {System.Net.WebUtility.HtmlEncode(restaurantName)}
                          </div>
                          {(!string.IsNullOrWhiteSpace(restaurantAddress) ? $"<div style='font-size:13px;color:#6b7280;margin-bottom:4px'>&#128205; {System.Net.WebUtility.HtmlEncode(restaurantAddress)}</div>" : "")}
                          {(!string.IsNullOrWhiteSpace(restaurantPhone) ? $"<div style='font-size:13px;color:#6b7280'>&#128222; {System.Net.WebUtility.HtmlEncode(restaurantPhone)}</div>" : "")}
                        </td>
                      </tr>
                    </table>"
                : "";

            return $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                      <meta charset='UTF-8'/>
                      <meta name='viewport' content='width=device-width,initial-scale=1'/>
                      <style>
                        @media only screen and (max-width:600px) {{
                          .email-wrapper {{ padding: 12px 0 !important; }}
                          .email-card {{ border-radius: 0 !important; width: 100% !important; }}
                          .card-body {{ padding: 20px !important; }}
                          .info-cell {{ display: block !important; width: 100% !important;
                                        padding: 4px 0 !important; box-sizing: border-box; }}
                          .info-inner {{ margin-bottom: 8px !important; }}
                        }}
                      </style>
                    </head>
                    <body style='margin:0;padding:0;background:#f3f4f6;font-family:Arial,sans-serif'>
                    <table width='100%' cellpadding='0' cellspacing='0' border='0'
                           class='email-wrapper' style='background:#f3f4f6;padding:40px 0'>
                      <tr>
                        <td align='center' style='padding:0 12px'>
                          <table width='100%' cellpadding='0' cellspacing='0' border='0'
                                 class='email-card'
                                 style='max-width:600px;background:#ffffff;border-radius:12px;overflow:hidden;
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
                              <td class='card-body' style='padding:32px'>

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
                                      <div style='font-size:28px;font-weight:700;letter-spacing:4px;color:#2563eb;word-break:break-all'>
                                        {confirmationCode}
                                      </div>
                                    </td>
                                  </tr>
                                </table>

                                <!-- DATE / TIME / GUESTS -->
                                <table width='100%' cellpadding='0' cellspacing='0' border='0'
                                       style='margin-bottom:20px'>
                                  <tr>
                                    <td class='info-cell' width='33%' style='padding-right:8px;vertical-align:top'>
                                      <table width='100%' cellpadding='12' cellspacing='0' border='0'
                                             class='info-inner'
                                             style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:10px'>
                                        <tr>
                                          <td style='text-align:center'>
                                            <div style='font-size:11px;color:#6b7280;letter-spacing:1px;margin-bottom:4px'>DATE</div>
                                            <div style='font-size:15px;font-weight:700;color:#111827'>{reservationDateTime:dd MMM yyyy}</div>
                                          </td>
                                        </tr>
                                      </table>
                                    </td>
                                    <td class='info-cell' width='33%' style='padding:0 4px;vertical-align:top'>
                                      <table width='100%' cellpadding='12' cellspacing='0' border='0'
                                             class='info-inner'
                                             style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:10px'>
                                        <tr>
                                          <td style='text-align:center'>
                                            <div style='font-size:11px;color:#6b7280;letter-spacing:1px;margin-bottom:4px'>TIME</div>
                                            <div style='font-size:15px;font-weight:700;color:#111827'>{reservationDateTime:HH:mm}</div>
                                          </td>
                                        </tr>
                                      </table>
                                    </td>
                                    <td class='info-cell' width='33%' style='padding-left:8px;vertical-align:top'>
                                      <table width='100%' cellpadding='12' cellspacing='0' border='0'
                                             class='info-inner'
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

                                {restaurantInfoSection}

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
        public static string TenantRequestSubmitted(
            string businessName,
            string contactEmail,
            string submittedAt)
        {
            var encodedBusiness = System.Net.WebUtility.HtmlEncode(businessName);
            return $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; background:#f3f4f6; padding:40px 0'>
                      <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                        <tr>
                          <td align='center'>
                            <table width='600' cellpadding='0' cellspacing='0' border='0'
                                   style='background:#ffffff;border-radius:12px;overflow:hidden;
                                          box-shadow:0 6px 24px rgba(0,0,0,0.06)'>

                              <!-- HEADER -->
                              <tr>
                                <td style='background:#111827;padding:28px;text-align:center'>
                                  <h1 style='margin:0;font-size:24px;color:#ffffff;font-weight:700'>
                                    &#128227; Tenant Request Received
                                  </h1>
                                </td>
                              </tr>

                              <!-- BODY -->
                              <tr>
                                <td style='padding:32px'>
                                  <p style='font-size:16px;color:#111827;margin:0 0 24px 0'>
                                    Dear <strong>{encodedBusiness}</strong>,
                                  </p>
                                  <p style='font-size:15px;color:#374151;line-height:1.7;margin:0 0 24px 0'>
                                    Thank you for submitting your tenant registration request. We have received your application
                                    and our team is currently reviewing it.
                                  </p>

                                  <table width='100%' cellpadding='16' cellspacing='0' border='0'
                                         style='background:#f9fafb;border:1px solid #e5e7eb;
                                                border-radius:10px;margin-bottom:24px'>
                                    <tr>
                                      <td>
                                        <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                                          <tr>
                                            <td style='padding-bottom:8px'>
                                              <span style='font-size:13px;color:#6b7280'>Business Name</span><br/>
                                              <span style='font-size:15px;font-weight:600;color:#111827'>{encodedBusiness}</span>
                                            </td>
                                          </tr>
                                          <tr>
                                            <td style='padding-bottom:8px'>
                                              <span style='font-size:13px;color:#6b7280'>Contact Email</span><br/>
                                              <span style='font-size:15px;font-weight:600;color:#111827'>{System.Net.WebUtility.HtmlEncode(contactEmail)}</span>
                                            </td>
                                          </tr>
                                          <tr>
                                            <td>
                                              <span style='font-size:13px;color:#6b7280'>Submitted At</span><br/>
                                              <span style='font-size:15px;font-weight:600;color:#111827'>{submittedAt}</span>
                                            </td>
                                          </tr>
                                        </table>
                                      </td>
                                    </tr>
                                  </table>

                                  <p style='font-size:14px;color:#6b7280;text-align:center'>
                                    You will receive another email once our team has reviewed your request.
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

        public static string TenantRequestAccepted(
            string businessName,
            string hostname)
        {
            var encodedBusiness = System.Net.WebUtility.HtmlEncode(businessName);
            var encodedHostname = System.Net.WebUtility.HtmlEncode(hostname);

            var contactSection = $@"
                    <table width='100%' cellpadding='16' cellspacing='0' border='0'
                           style='background:#f0fdf4;border:1px solid #bbf7d0;
                                  border-radius:10px;margin-top:16px'>
                      <tr>
                        <td>
                          <div style='font-size:14px;font-weight:700;color:#15803d;margin-bottom:8px'>
                            &#128222; Contact Support
                          </div>
                          <div style='font-size:14px;color:#374151'>Email: <a href='mailto:baook01234@gmail.com' style='color:#2563eb;text-decoration:none'>baook01234@gmail.com</a></div>
                          <div style='font-size:14px;color:#374151'>Phone: 0987367341</div>
                        </td>
                      </tr>
                    </table>";

            return $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; background:#f3f4f6; padding:40px 0'>
                      <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                        <tr>
                          <td align='center'>
                            <table width='600' cellpadding='0' cellspacing='0' border='0'
                                   style='background:#ffffff;border-radius:12px;overflow:hidden;
                                          box-shadow:0 6px 24px rgba(0,0,0,0.06)'>

                              <!-- HEADER -->
                              <tr>
                                <td style='background:#15803d;padding:28px;text-align:center'>
                                  <h1 style='margin:0;font-size:24px;color:#ffffff;font-weight:700'>
                                    &#9989; Tenant Request Accepted
                                  </h1>
                                </td>
                              </tr>

                              <!-- BODY -->
                              <tr>
                                <td style='padding:32px'>
                                  <p style='font-size:16px;color:#111827;margin:0 0 24px 0'>
                                    Dear <strong>{encodedBusiness}</strong>,
                                  </p>
                                  <p style='font-size:15px;color:#374151;line-height:1.7;margin:0 0 24px 0'>
                                    Great news! Your tenant registration request has been
                                    <strong style='color:#15803d'>approved</strong>. Your tenant account is now active.
                                  </p>

                                  <table width='100%' cellpadding='16' cellspacing='0' border='0'
                                         style='background:#eff6ff;border:1px solid #bfdbfe;
                                                border-radius:10px;margin-bottom:24px;text-align:center'>
                                    <tr>
                                      <td>
                                        <div style='font-size:13px;color:#6b7280;letter-spacing:1px;margin-bottom:8px'>
                                          YOUR TENANT PORTAL
                                        </div>
                                        <a href='https://{encodedHostname}'
                                           style='display:inline-block;background:#2563eb;color:#ffffff;
                                                  text-decoration:none;padding:13px 32px;
                                                  border-radius:8px;font-size:15px;font-weight:600'>
                                          Your Restaurant
                                        </a>
                                        <div style='margin-top:10px;font-size:13px;color:#6b7280'>
                                          {encodedHostname}
                                        </div>
                                      </td>
                                    </tr>
                                  </table>

                                  {contactSection}

                                  <p style='margin-top:24px;font-size:13px;color:#9ca3af;text-align:center'>
                                    Keep this email for your records.
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

        public static string TenantRequestRejected(string businessName)
        {
            var encodedBusiness = System.Net.WebUtility.HtmlEncode(businessName);

            return $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; background:#f3f4f6; padding:40px 0'>
                      <table width='100%' cellpadding='0' cellspacing='0' border='0'>
                        <tr>
                          <td align='center'>
                            <table width='600' cellpadding='0' cellspacing='0' border='0'
                                   style='background:#ffffff;border-radius:12px;overflow:hidden;
                                          box-shadow:0 6px 24px rgba(0,0,0,0.06)'>

                              <!-- HEADER -->
                              <tr>
                                <td style='background:#1f2937;padding:28px;text-align:center'>
                                  <h1 style='margin:0;font-size:24px;color:#ffffff;font-weight:700'>
                                    &#9888; Application Under Review
                                  </h1>
                                </td>
                              </tr>

                              <!-- BODY -->
                              <tr>
                                <td style='padding:32px'>
                                  <p style='font-size:16px;color:#111827;margin:0 0 24px 0'>
                                    Dear <strong>{encodedBusiness}</strong>,
                                  </p>
                                  <p style='font-size:15px;color:#374151;line-height:1.7;margin:0 0 16px 0'>
                                    Thank you for your interest in <strong>RestX</strong>. After a thorough review of your
                                    application, we were unable to approve it at this time.
                                  </p>
                                  <p style='font-size:15px;color:#374151;line-height:1.7;margin:0 0 24px 0'>
                                    Our team evaluates each submission carefully to ensure platform quality and compliance
                                    with our service standards. Unfortunately, your application does not currently meet
                                    our requirements.
                                  </p>

                                  <table width='100%' cellpadding='0' cellspacing='0' border='0'
                                         style='background:#f9fafb;border-left:4px solid #d97706;
                                                border-radius:6px;margin-bottom:24px'>
                                    <tr>
                                      <td style='padding:14px 18px'>
                                        <div style='font-size:14px;font-weight:600;color:#92400e;margin-bottom:4px'>
                                          What's next?
                                        </div>
                                        <div style='font-size:14px;color:#78350f;line-height:1.6'>
                                          You are welcome to review our
                                          <a href='https://restx.food/' style='color:#2563eb;text-decoration:none'>platform guidelines</a>
                                          and resubmit a new application in the future.
                                          Our team is happy to assist if you have any questions.
                                        </div>
                                      </td>
                                    </tr>
                                  </table>

                                  <table width='100%' cellpadding='16' cellspacing='0' border='0'
                                         style='background:#f0fdf4;border:1px solid #bbf7d0;
                                                border-radius:10px;margin-bottom:24px'>
                                    <tr>
                                      <td>
                                        <div style='font-size:14px;font-weight:700;color:#15803d;margin-bottom:8px'>
                                          &#128222; Contact Support
                                        </div>
                                        <div style='font-size:14px;color:#374151'>Email: <a href='mailto:baook01234@gmail.com' style='color:#2563eb;text-decoration:none'>baook01234@gmail.com</a></div>
                                        <div style='font-size:14px;color:#374151'>Phone: 0987367341</div>
                                      </td>
                                    </tr>
                                  </table>

                                  <p style='font-size:14px;color:#6b7280;text-align:center'>
                                    If you believe this was a mistake or would like further clarification,
                                    please reply to this email or contact our support team.
                                  </p>
                                </td>
                              </tr>

                              <!-- FOOTER -->
                              <tr>
                                <td style='background:#f9fafb;padding:16px;text-align:center;
                                           font-size:12px;color:#9ca3af;border-top:1px solid #e5e7eb'>
                                  This is an automated email. Please do not reply directly to this message.
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
