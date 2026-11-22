# Family policy command authorization

Family schedule/app/web commands require a compatible companion client. Legacy clients may still view summaries and use supported session/time-request features, but must not be allowed to bypass Guardian authorization to mutate protected policy.

## Enrollment

Open the phone hub with the administrator PIN. Creating a new invitation or authorizing remote management pins the relay room/key in Guardian. Revocation removes that authority. Protected policy file ACLs remain read-only for the managed user.

## PIN authorization

The snapshot advertises `guardian-pin-v2` for Family policies. The phone stores the Guardian RSA public key from the QR invitation shown after desktop PIN authorization. The relay snapshot cannot change that key. Existing Family phones must scan a new QR invitation before editing policy. The RSA private key stays in Guardian's access-controlled enrollment.

The parent enters the 4–8 digit administrator PIN on the phone for each policy change. The phone keeps the PIN only long enough to encrypt the request.

Construct the following UTF-8 text, joined by LF with no trailing LF:

1. kvieta-parent-policy-v2
2. relay room
3. command sequence in invariant decimal
4. decisionId
5. requestId (empty if not used)
6. action (update-plan, update-app-rule, or update-web-guard)
7. grantedMinutes in invariant decimal, or empty
8. decidedAtUtc as Unix milliseconds in invariant decimal
9. standard Base64 of the exact UTF-8 payloadJson string (empty string if absent)

Hash this text with SHA-256, append the ASCII PIN, and encrypt the result with the paired RSA key using OAEP with SHA-256 and MGF1-SHA-256. Include the Base64 ciphertext as `parentProof` inside the encrypted decision payload. The digest binds the PIN authorization to the exact command.

Guardian checks room, freshness, sequence, command digest, and the PIN against its protected credential before applying the mutation. Rejected authorization attempts share the Guardian PIN throttle. Commands expire after five minutes. A retry uses the identical encrypted decision and its original payload.

The additional remoteCommand snapshot field contains the last PC receipt: Applying, Applied, Failed or Expired, with decisionId/sequence and a safe error string. A failed command remains retryable; an Applied receipt persists across process restart. Android rendering and device-level acceptance are required before advertising Family policy editing for that client version.
