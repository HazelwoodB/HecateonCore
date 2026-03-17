# Sync Two-Device Harness (A↔B)

This harness validates Phase 3 requirements:
- offline queue persisted to disk
- pull updates `LastSeenSeq`
- push handles accepted/duplicate/rejected outcomes
- retryable failures use exponential backoff with cap
- deterministic no-duplicate behavior across two devices

## Preconditions
- Server running locally
- Device A and Device B approved in trusted device registry
- Use `X-Device-Id` header per device

## Key Endpoints
- `POST /api/sync/queue/events`
- `POST /api/sync/push/{stream}?maxItems=...`
- `POST /api/sync/pull/{stream}?limit=...`
- `GET /api/sync/status`
- `GET /streams/{stream}/head`
- `GET /streams/{stream}/events?sinceSeq=&limit=`

## Test Data
Use stream `chat` and deterministic `ClientMsgId` values:
- `A-msg-001`
- `A-msg-002`
- `B-msg-001`

## Procedure
1. **A offline enqueue**
   - Queue 2 envelopes on Device A via `/api/sync/queue/events`
   - Confirm `/api/sync/status` shows queue size +2

2. **A push while online**
   - Call `/api/sync/push/chat`
   - Verify result has accepted items
   - Confirm queue size decrements to 0

3. **B pull**
   - Call `/api/sync/pull/chat`
   - Verify events returned ordered by `Seq`
   - Verify `lastSeenSeq` advanced

4. **Duplicate protection**
   - Re-queue Device A envelope using same `(UserId,DeviceId,ClientMsgId)`
   - Push again and verify disposition `duplicate`

5. **B offline then sync**
   - Queue `B-msg-001` while disconnected
   - Later call `/api/sync/push/chat`
   - On Device A call `/api/sync/pull/chat` and verify exactly one new event

6. **Head consistency**
   - Compare `/streams/chat/head` to max `Seq` observed from pulls

7. **Backoff behavior**
   - Force a retryable rejection path (for example: temporary network failure to authority)
   - Call `/api/sync/push/chat` and confirm response may return `noDueItems=true` on immediate retry
   - Wait for backoff window and retry push; verify queued item is attempted again

## Expected Pass Criteria
- No duplicate records for same idempotency tuple
- Pull responses are monotonic by `Seq`
- `lastSeenSeq` never regresses
- Queue survives process restart (state stored in `App_Data/sync-state.json`)
- Retry interval increases per attempt (capped exponential backoff)
