import { RefundPolicyLabelPipe } from './refund-policy-label.pipe';

describe('RefundPolicyLabelPipe', () => {
  const pipe = new RefundPolicyLabelPipe();

  it('returns the 80% tier label when departure is 24h or more away', () => {
    expect(pipe.transform(25)).toBe('80% refund (24h or more before departure)');
    expect(pipe.transform(24)).toBe('80% refund (24h or more before departure)');
  });

  it('returns the 50% tier label when departure is between 12h and 24h', () => {
    expect(pipe.transform(23.9)).toBe('50% refund (12h–24h before departure)');
    expect(pipe.transform(12)).toBe('50% refund (12h–24h before departure)');
  });

  it('returns the blocked label when departure is under 12h', () => {
    expect(pipe.transform(11.9)).toBe('Cancellation not allowed (under 12h to departure)');
    expect(pipe.transform(0)).toBe('Cancellation not allowed (under 12h to departure)');
  });

  it('returns the blocked label for negative hours (past departure)', () => {
    expect(pipe.transform(-1)).toBe('Cancellation not allowed (under 12h to departure)');
  });
});
