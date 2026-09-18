import {getApiErrorMessage} from './api-error';

describe('getApiErrorMessage',()=>{
  it('prefers the server message',()=>{
    expect(getApiErrorMessage({error:{errorCode:'INVALID_EVENT',message:'Risk limit rejected event.'}},'fallback')).toBe('Risk limit rejected event.');
  });

  it('falls back to the stable server error code',()=>{
    expect(getApiErrorMessage({error:{errorCode:'PERSISTENCE_DISABLED'}},'fallback')).toBe('PERSISTENCE_DISABLED');
  });

  it('uses the local fallback for malformed errors',()=>{
    expect(getApiErrorMessage({error:null},'Unable to load session.')).toBe('Unable to load session.');
    expect(getApiErrorMessage(new Error('network'), 'Network unavailable.')).toBe('Network unavailable.');
  });
});
