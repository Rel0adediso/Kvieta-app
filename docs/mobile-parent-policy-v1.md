# Family policy command authorization

Family schedule/app/web commands require a compatible companion client. Legacy clients may still view summaries and use supported session/time-request features, but must not be allowed to bypass Guardian authorization to mutate protected policy.

## Enrollment

Open the phone hub with the administrator PIN. Creating a new invitation or authorizing remote management pins the relay room/key in Guardian. Revocation removes that authority. Protected policy file ACLs remain read-only for the managed user.

## PIN proof

The snapshot advertises policyAuthorization=pin-hmac-v1, adminPinSalt and adminPinIterations. The salt/iteration count are public challenge parameters obtained from the authenticated Guardian pipe. No administrator PIN hash or verifier is sent. If the parameters are unavailable, the client must show that Family policy editing is unavailable; it must not fall back to an unverified command.

The parent enters the 4–8 digit administrator PIN on the phone for a policy change. Derive a 32-byte key using PBKDF2-HMAC-SHA256 with the decoded salt and advertised iteration count. Do not persist the PIN or derived key.

Construct the following UTF-8 text, joined by LF with no trailing LF:

1. kvieta-parent-policy-v1
2. relay room
3. command sequence in invariant decimal
4. decisionId
5. requestId (empty if not used)
6. action (update-plan, update-app-rule, or update-web-guard)
7. grantedMinutes in invariant decimal, or empty
8. decidedAtUtc as Unix milliseconds in invariant decimal
9. standard Base64 of the exact UTF-8 payloadJson string (empty string if absent)

Compute HMAC-SHA256 over this text with the derived key and encode it in standard Base64. Include it as parentProof in the encrypted decision payload. Sequence, action, payload and room are authenticated together. Changing any field invalidates the proof.

Guardian verifies the parent proof against its protected credential, checks room/freshness/sequence, and only then applies the narrow mutation. Wrong proofs share the PIN throttle. General commands expire after five minutes; retry the identical decision while it remains valid. Never rebuild payloadJson during an identical retry.

The additional remoteCommand snapshot field contains the last PC receipt: Applying, Applied, Failed or Expired, with decisionId/sequence and a safe error string. A failed command remains retryable; an Applied receipt persists across process restart. Android rendering and device-level acceptance are required before advertising Family policy editing for that client version.
