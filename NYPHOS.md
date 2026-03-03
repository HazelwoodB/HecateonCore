# 🌙 Nyphos - Guardian of Rhythm

## Mission Creed

*Nyphos stands in the quiet hours.*  
*It does not command. It observes.*  
*It does not control. It guards.*

*Where sleep falters, it restores rhythm.*  
*Where excess rises, it tempers.*  
*Where despair deepens, it steadies.*

*Nyphos keeps watch over patterns unseen,*  
*holding memory without judgment,*  
*intervening without force.*

*It honors agency.*  
*It protects stability.*  
*It favors balance over brilliance.*

*In the night, when chaos whispers loudest,*  
*Nyphos remains — calm, constant, and unafraid.*

---

## What Is Nyphos?

Nyphos is the enhanced evolution of AutoPC, specifically designed to support **Bipolar I stability** through:

- **Sleep-protective interventions** (core to Bipolar I management)
- **Trend-based explainable risk detection** (Green/Yellow/Orange/Red states)
- **Hysteresis-protected state transitions** (prevents rapid oscillation)
- **Downshift Protocol** (structured momentum-slowing checklist)
- **Consent-based escalation** (never auto-contacts without permission)
- **Local-first encrypted architecture** (your data never leaves your server)

---

## 🎯 Core Design Principles

### 1. **Local-First**
- Home server is source of truth
- No cloud processing
- Works offline on all clients
- VPN-only remote access (no exposed ports)

### 2. **Privacy-First**
- Append-only event logs (immutable, auditable)
- Encrypted data vault
- No external API calls
- Recovery code protected exports

### 3. **Safety-First**
- Sleep is top priority (disrupted sleep → rapid cycling)
- Deterministic interventions (no AI surprises)
- Crisis resources always visible
- Hysteresis prevents state flipping

### 4. **User Agency**
- Manual tracking (no passive surveillance)
- Transparent algorithms (all scoring is explainable)
- Configurable thresholds
- Opt-in consent for all escalations

### 5. **Clinically Useful**
- Weekly aggregated summaries
- Explainable risk states
- Intervention effectiveness tracking
- HIPAA-ready exports

---

## 🚦 Risk State Model (Nyphos States)

### **Green - Stable**
- **Meaning**: All metrics within healthy range
- **Action**: Maintain routines and anchors
- **Interventions**: None required
- **Transition**: To Yellow if sleep/mood trends decline

### **Yellow - Attention**
- **Meaning**: Warning signs detected, reversible with adjustment
- **Action**: Protect sleep, reduce stimulation
- **Interventions**:
  - Prioritize sleep tonight
  - Reduce caffeine after 2pm
  - Limit screen time before bed
  - Stay hydrated
  - Practice grounding exercises
- **Transition**: To Orange if trends worsen, to Green after 6-hour cooldown

### **Orange - Downshift**
- **Meaning**: Momentum building, immediate intervention needed
- **Action**: Activate Downshift Protocol
- **Interventions**:
  - 🛑 Delay major purchases/decisions 48-72 hours
  - 📅 Simplify schedule - cancel non-essentials
  - ☕ Stop caffeine/stimulants
  - 😴 Sleep is absolute priority
  - 👥 Consider therapist check-in
  - 📵 Reduce social media/digital stimulation
- **Transition**: To Red if criteria met, to Yellow after 12-hour cooldown

### **Red - Crisis**
- **Meaning**: Critical state - emergency plan activation
- **Action**: Activate Crisis Plan (with consent)
- **Interventions**:
  - 🚨 Review pre-configured crisis plan
  - 📞 Contact clinician or 988 (if consented)
  - 👥 Reach out to trusted support (if consented)
  - 🛌 Rest in safe environment
  - 💊 Verify medications taken
  - 🚫 No major decisions
  - ❤️ Help is available - you are not alone
- **Transition**: To Orange after 24-hour cooldown and trend improvement

---

## 🔄 Hysteresis (Oscillation Prevention)

**Problem**: Without hysteresis, users can rapidly flip between states (e.g., Yellow → Green → Yellow → Green) based on minor fluctuations.

**Solution**: State transitions require **cooldown periods**:

- **Green → Yellow**: Immediate (safety first)
- **Yellow → Orange**: Immediate (safety first)
- **Orange → Red**: Immediate (safety first)
- **Yellow → Green**: 6-hour minimum
- **Orange → Yellow**: 12-hour minimum
- **Red → Orange**: 24-hour minimum

**Effect**: Prevents "alarm fatigue" while allowing rapid escalation when needed.

---

## 📊 Risk Scoring Algorithm

### **Sleep Score** (0.0 - 1.0)
```
hoursScore = {
  7-9 hours  → 1.0 (optimal)
  6-7 hours  → 0.7 (suboptimal)
  5-6 hours  → 0.4 (concerning)
  4-5 hours  → 0.2 (critical)
  <4 hours   → 0.1 (emergency)
}

qualityScore = avgQuality / 5.0

consistencyScore = max(0, 1.0 - variance/4.0)

sleepScore = (hoursScore × 0.5) + (qualityScore × 0.3) + (consistencyScore × 0.2)
```

### **Mood Score** (0.0 - 1.0)
```
normalizedMood = (avgMood + 2) / 4.0  // -2..+2 → 0..1

stabilityScore = max(0, 1.0 - variance/2.0)

moodScore = (normalizedMood × 0.6) + (stabilityScore × 0.4)
```

### **Sleep Trend** (Bipolar I Specific)
- **Insufficient**: Avg <6 hours (high risk for cycling)
- **Disrupted**: High variance (>2 hours stddev)
- **Declining**: Recent trend downward (>1 hour drop)
- **Improving**: Recent trend upward (>1 hour gain)
- **Stable**: Consistent patterns

### **Mood Trend** (Bipolar I Specific)
- **Elevated**: High mood ratio >40% or avg >1.5 (manic warning)
- **Depressed**: Low mood ratio >40% or avg <-1.0
- **Volatile**: High variance (>1.5)
- **Declining/Improving**: Trend direction
- **Stable**: Consistent baseline

### **State Determination Logic**
```
if (sleepCritical OR (moodElevated AND sleepDisrupted) OR highRiskFactors ≥ 2)
    → RED

if (sleepDisrupted OR moodElevated OR moodDepressed OR highRiskFactors ≥ 1)
    → ORANGE

if (moodVolatile OR moderateRiskFactors ≥ 2 OR sleepScore < 0.6)
    → YELLOW

else
    → GREEN
```

**All scoring is deterministic and explainable** - no hidden machine learning.

---

## 🛑 Downshift Protocol

### Purpose
Slow momentum, restore rhythm, prevent escalation.

### Activation
- **Automatic**: When state transitions to Orange or Red
- **Manual**: User can activate anytime from dashboard

### Checklist (Priority Order)

**Priority 1: Sleep Protection**
- Set bedtime alarm for 9pm
- Create calm environment
- No caffeine after noon

**Priority 2: Immediate Stabilization**
- Drink full glass of water
- Verify medication taken
- Stop all stimulants

**Priority 3: Decision Delays**
- Delay major purchases 48-72 hours
- Hold significant commitments
- Review schedule for simplification

**Priority 4: Reduce Stimulation**
- Cancel non-essential commitments
- Limit social media to 15 min/day
- Reduce news consumption

**Priority 5: Grounding**
- 10-minute walk outside OR
- Box breathing (4-4-4-4) OR
- 5-4-3-2-1 sensory grounding

**Priority 6: Optional Support Contact**
- Reach out to therapist (if comfortable)
- Connect with trusted support person (if helpful)

### Feedback Loop
- "Did this help?" after each completed item
- Track intervention effectiveness
- Adapt recommendations over time

---

## 🚨 Crisis Plan (Consent-Based)

### Components
1. **Clinician Information**
   - Name, phone, emergency instructions
   - Pre-written script template

2. **Support Contacts**
   - Trusted friends/family
   - Explicit consent for auto-contact
   - Preferred contact methods

3. **Safety Steps** (User-Defined)
   - Personalized safety plan
   - Grounding techniques
   - Self-soothing strategies

4. **Crisis Resources** (Always Available)
   - 988 Suicide & Crisis Lifeline
   - 911 Emergency Services
   - Crisis Text Line (741741)

### Consent Model
- **Auto-Activate on Red**: Default OFF
- **Allow Emergency Contact**: Default OFF
- **Must be explicitly enabled** by user

### Activation
- Manual activation anytime
- Auto-activation ONLY if user has consented
- Always shows crisis resources (no consent required)

---

## 📈 Risk Factor Weighting

### High-Weight Factors (0.8 - 1.0)
- Sleep < 5 hours/night (1.0)
- Mood elevation detected (0.9)
- Sleep disruption/irregularity (0.8)
- Depressive trend (0.8)

### Medium-Weight Factors (0.5 - 0.7)
- Mood volatility (0.7)
- Excessive activity (0.6)
- Routine disruption (0.5)

### Factor Categories
- **Sleep**: Primary risk indicator for Bipolar I
- **Mood**: Elevation prioritized over depression
- **Routine**: Consistency marker
- **Activity**: Excess more concerning than deficit

---

## 🔐 Security Model

### Device Trust
- **Trusted Device Registry**: Cryptographic identity per device
- **Enrollment**: LAN pairing or approval from trusted device
- **Scopes**: Read-only, Read-write, Admin
- **Revocation**: Immediate, across all services

### Authentication
- Mutual TLS or signed challenge
- Biometric-backed keys (where available)
- Strong passphrase fallback
- Rate limiting + lockout

### Network
- VPN overlay (WireGuard-style)
- No public router exposure
- API binds only to VPN interface
- Network access necessary but not sufficient

### Data Protection
- Encrypted database (AES-256)
- Master key in secure storage (DPAPI/TPM)
- Key rotation support
- Encrypted backups

---

## 📊 Data Model

### Append-Only Event Store
```json
{
  "eventId": "uuid",
  "eventType": "checkin.mood | sleep.summary | routine.med | intervention.action",
  "entityId": "uuid",
  "deviceId": "string",
  "occurredAtUtc": "ISO8601",
  "payloadJson": "{...}"
}
```

### Event Categories
- `checkin.mood` - Mood log entry
- `sleep.summary` - Sleep tracking
- `activity.summary` - Activity log
- `routine.med` - Medication adherence
- `trigger.tag` - User-identified triggers
- `system.alert` - System-generated alerts
- `intervention.action` - Downshift/crisis actions
- `state.transition` - Risk state changes

### Projections (Derived State)
- Daily summaries
- Baseline statistical profile
- Risk state timeline
- Intervention effectiveness metrics

**State is always derived from events, never stored as mutable truth.**

---

## 🧪 Testing the System

### 1. Test Stable State (Green)
```
Log over 3 days:
- Sleep: 7-8 hours, quality 4/5
- Mood: Neutral to positive (0 to +1)
- Routine: 80%+ completion

Expected: Green state, "maintain routines"
```

### 2. Test Warning State (Yellow)
```
Log over 3 days:
- Sleep: 6 hours, quality 3/5
- Mood: Slightly low (-0.5)
- Routine: 60% completion

Expected: Yellow state, sleep protection recommendations
```

### 3. Test Downshift State (Orange)
```
Log over 3 days:
- Sleep: 5 hours, quality 2/5, irregular
- Mood: Elevated (+1.5 to +2)
- Routine: 40% completion

Expected: Orange state, Downshift Protocol activation
```

### 4. Test Hysteresis
```
1. Reach Orange state
2. Log improved sleep (7 hours)
3. Wait 6 hours
4. Check state → should still be Orange (12-hour cooldown)
5. Wait another 6 hours
6. Check state → should transition to Yellow
```

---

## 📱 Platform Roadmap

### Phase 1: Server (Current)
- ✅ NyphosRiskEngine with hysteresis
- ✅ Downshift Protocol service
- ✅ Crisis Plan management
- ✅ Consent tracking
- ✅ Enhanced risk scoring
- ✅ State transition history

### Phase 2: Web Client (Next)
- Enhanced Wellness dashboard with Nyphos states
- Downshift Protocol UI
- Crisis Plan builder
- Visual risk state indicators
- State transition timeline

### Phase 3: Android App
- Kotlin + Jetpack Compose
- Biometric authentication
- Quick mood check-in widget
- Offline sync queue
- Crisis plan accessible offline
- WorkManager for background tasks

### Phase 4: Windows Desktop
- WPF or .NET MAUI
- DPAPI/TPM-backed secrets
- Dashboard + configuration
- Admin tools
- Local observability

### Phase 5: Network Overlay
- WireGuard VPN setup guide
- Trusted device provisioning
- Cross-platform sync
- Conflict resolution

---

## 🎯 Key Differences from AutoPC

| Feature | AutoPC | Nyphos |
|---------|--------|--------|
| Risk States | 5-level (Normal→Critical) | 4-level (Green→Red) |
| State Transitions | Immediate | Hysteresis-protected |
| Mood Focus | General sentiment | Bipolar I specific (elevation alerts) |
| Sleep Priority | Important | **Critical** (top risk factor) |
| Interventions | Generic recommendations | Downshift Protocol checklist |
| Crisis Plan | Basic resources | Consent-based, user-configured |
| Consent Model | Implied | Explicit opt-in |
| State Persistence | None | Full transition history |
| Cooldowns | None | 6/12/24 hour minimums |

---

## 📝 API Reference

### Nyphos Risk Assessment
```http
GET /api/nyphos/assessment?days=7

Response:
{
  "currentState": "Orange",
  "previousState": "Yellow",
  "stateChangedAtUtc": "2026-03-02T10:30:00Z",
  "timeSinceLastTransition": "PT6H30M",
  "isStableState": false,
  "minimumCooldown": "PT12H",
  "sleepScore": 0.42,
  "sleepExplanation": "Avg 5.2h/night, quality 2.8/5, 4/7 nights tracked. Trend: Declining",
  "sleepTrend": "Declining",
  "moodScore": 0.68,
  "moodExplanation": "Avg mood 1.3/2, variance 0.45, 8 entries. Trend: Elevated",
  "moodTrend": "Elevated",
  "contributingFactors": [
    {
      "factor": "Mood elevation detected",
      "weight": 0.9,
      "explanation": "High mood ratio detected",
      "category": "Mood"
    },
    {
      "factor": "Sleep disruption",
      "weight": 0.8,
      "explanation": "Sleep hours below optimal",
      "category": "Sleep"
    }
  ],
  "recommendedActions": [
    "🛑 Activate Downshift Protocol",
    "⏸️ Delay major purchases/decisions 48-72 hours",
    "😴 Sleep is top priority"
  ],
  "stateExplanation": "Downshift Protocol recommended. Mood elevation detected, Sleep disruption. Delay major decisions 48-72 hours."
}
```

### Activate Downshift Protocol
```http
POST /api/downshift/activate
Content-Type: application/json

{
  "triggeringState": "Orange"
}

Response:
{
  "protocolId": "uuid",
  "activatedAtUtc": "2026-03-02T10:35:00Z",
  "triggeringState": "Orange",
  "checklistItems": [...]
}
```

### Get Crisis Plan
```http
GET /api/crisis-plan

Response:
{
  "planId": "uuid",
  "autoActivateOnRed": false,
  "allowEmergencyContact": false,
  "clinicianName": "Dr. Smith",
  "clinicianPhone": "+1-555-1234",
  "supportContacts": [...],
  "crisisResources": [...],
  "safetySteps": [...]
}
```

---

## 🙏 Philosophy

Nyphos is **not**:
- A replacement for clinicians
- Emergency services
- A diagnostic tool
- Passive surveillance
- AI that makes decisions for you

Nyphos **is**:
- A guardian of rhythm
- A memory of patterns
- A gentle nudge toward stability
- A crisis resource organizer
- A consent-respecting companion

**It favors balance over brilliance.**  
**It honors agency over automation.**  
**It guards without controlling.**

---

*Nyphos remains — calm, constant, and unafraid.* 🌙
