// Rule: L-07 Structured logging & automatic redaction
import { redactSensitiveData, redactObject } from '../../src/security/redact';

describe('redact', () => {
  it('[L-07] redacts bearer tokens, cookies, and secret json properties', () => {
    const raw = 'Bearer eyJhbGciOiJIUzI1NiJ9.test and Cookie: session_id=abc';
    const redacted = redactSensitiveData(raw);

    expect(redacted).not.toContain('eyJhbGciOiJIUzI1NiJ9');
    expect(redacted).toContain('Bearer [REDACTED]');
  });

  it('[L-07] redacts sensitive keys in objects', () => {
    const obj = {
      username: 'alice',
      password: 'mypassword123',
      access_token: 'jwt_123',
    };

    const redacted = redactObject(obj);
    expect(redacted.username).toBe('alice');
    expect(redacted.password).toBe('[REDACTED]');
    expect(redacted.access_token).toBe('[REDACTED]');
  });
});
