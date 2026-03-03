# HECATEONCORE MASTER ROADMAP
## Unified Platform Development Plan

**Status**: Active Development  
**Last Updated**: January 2025  
**Current Phase**: Phase B/C Transition

---

## 🎯 STRATEGIC REALITY

**You do NOT have multiple projects. You have:**

```
HecateonCore = The Platform
├── Hecateon Desktop = The Local Command Client (Primary)
├── Web Dashboard = Secondary Surface
├── NYPHOS Module = Stability Engine (✅ Complete)
└── Prometheon Module = Cognitive Engine (📋 Planned)
```

---

## 📊 MASTER PHASES

### **PHASE A: PLATFORM CONSOLIDATION** ✅ **COMPLETE**

**Goal**: Unify naming and architecture under HecateonCore.

**Completed Actions**:
- ✅ HecateonCore established as canonical platform name
- ✅ Repository migrated from AutoPC to HecateonCore
- ✅ Desktop app becomes "Hecateon Desktop"
- ✅ NYPHOS remains as module name
- ✅ Prometheon positioned as future module inside HecateonCore
- ✅ Architecture extended, not forked

**Status**: **DONE** ✅

---

### **PHASE B: DESKTOP-FIRST AUTHORITY MODEL** ✅ **COMPLETE**

**Goal**: Establish Desktop as primary control plane.

**Architecture Established**:
- ✅ Desktop app is the primary control plane
- ✅ Web app is secondary surface / remote dashboard
- ✅ Web consumes state via API (not the authority)

**Implementation**:
- ✅ Prometheon engine will run locally (when built)
- ✅ State engine lives in Desktop process
- ✅ Web app consumes state via API
- ✅ Unreal bridge attaches to local engine (future)
- ✅ No duplication of engines

**Status**: **ARCHITECTURE COMPLETE** ✅  
**Next**: Begin Prometheon module integration

---

### **PHASE C: PROMETHEON MODULE INTEGRATION** 🚧 **IN PROGRESS**

**Goal**: Integrate Prometheon cognitive engine using existing HecateonCore principles.

#### **Core Principles (From `.github/copilot-instructions.md`)**:
1. ✅ Append-only storage
2. ✅ Deterministic logic
3. ✅ Versioned models
4. ✅ Hysteresis
5. ✅ Explicit consent
6. ✅ Audit logging

#### **Prometheon Requirements**:
- 📋 Transaction events are append-only
- 📋 Narrative reframes are append-only
- 📋 Foresight simulations are logged
- 📋 No hidden AI mutation
- 📋 All projections reproducible

#### **Implementation Plan**:

**Step 1: Define Prometheon Core Models** 📝
- `Modules/Prometheon/Models/`
  - `OperatorState.cs` - Child/Adult/Parent ego states
  - `Transaction.cs` - Transactional Analysis events
  - `NarrativeFrame.cs` - Reframe events
  - `ForesightSimulation.cs` - Decision projection
  - `CognitiveEvent.cs` - Base event type

**Step 2: Build Prometheon Engine** 🔧
- `Modules/Prometheon/Services/`
  - `PrometheronEngine.cs` - Main cognitive operator
  - `TransactionalAnalyzer.cs` - TA pattern detection
  - `NarrativeReframer.cs` - Cognitive restructuring
  - `ForesightSimulator.cs` - Decision projection
  - `OperatorChat.cs` - Structured dialogue

**Step 3: Create Append-Only Event Store** 💾
- `Core/EventStore/PrometheonEventStore.cs`
- Inherits from existing `IEventStore`
- SQLite encrypted storage
- No mutation - only append

**Step 4: Build Desktop UI Integration** 🎨
- `Lullaby.Desktop/Views/PrometheronView.xaml`
- Operator state visualization
- Transaction log display
- Foresight simulation UI
- Chat interface

**Step 5: Testing & Validation** ✅
- Unit tests for all Prometheon logic
- Integration tests with NYPHOS
- Determinism verification
- Hysteresis validation

**Status**: **READY TO BEGIN** 🚀  
**Next Action**: Create Prometheon module structure

---

### **PHASE D: SAFETY SYSTEMS EXPANSION** 📋 **PLANNED**

**Goal**: Unified safety ladder across NYPHOS + Prometheon.

#### **Current State**:
- ✅ NYPHOS has risk engine
- ✅ NYPHOS has intervention ladder
- ✅ Crisis UI exists

#### **Expansion Plan**:
- 📋 NYPHOS handles: Biological destabilization
- 📋 Prometheon handles: Cognitive destabilization
- 📋 Unified escalation ladder
- 📋 If both escalate → crisis UI intensifies

#### **Implementation**:
```
Risk Assessment:
├── NYPHOS Risk Score (0-100)
├── Prometheon Risk Score (0-100)
└── Combined Risk Index → Unified Intervention

Intervention Ladder:
1. Green: Normal operation
2. Yellow: Gentle prompts
3. Orange: Active intervention
4. Red: Crisis protocol
```

**Status**: **BLOCKED BY PHASE C**  
**Next**: Complete Prometheon integration first

---

### **PHASE E: TRUSTED REMOTE ACCESS** 📋 **PLANNED**

**Goal**: VPN-only remote access with module-level scopes.

#### **From Roadmap** (`HECATEONCORE_PROJECT_OVERVIEW.md`):
- VPN integration
- Trusted device management
- Secure sync protocol

#### **Prometheon Integration Requirements**:
- 📋 Scope-based module access
- 📋 Prometheon read/write scopes
- 📋 Device-level isolation
- 📋 Consent logs for narrative exports
- 📋 Remote access cannot bypass module isolation

**Implementation**:
```
Device Registry:
├── Trusted Device ID
├── Module Scopes (NYPHOS, Prometheon)
├── Access Level (Read, Write, Admin)
└── Consent Boundaries

VPN Requirement:
- No remote access without VPN
- Home network as authority
- All remote sync encrypted
```

**Status**: **WAITING FOR PHASE D**

---

### **PHASE F: CLINICAL EXPORT EXTENSION** 📋 **PLANNED**

**Goal**: Extend export model to include cognitive layer.

#### **Current Export** (Partial):
- ✅ Weekly summary structure exists
- 📋 Clinician-ready reports (incomplete)
- 📋 Standard formats (incomplete)

#### **Extended Export Model**:
```
Weekly Export:
├── NYPHOS Section:
│   ├── Sleep patterns
│   ├── Mood trends
│   ├── Routine adherence
│   └── Risk flags
└── Prometheon Section:
    ├── Narrative patterns
    ├── Transaction conflicts
    ├── Foresight risk flags
    └── Cognitive escalation events
```

#### **Export Requirements**:
- 📋 Consent-gated (explicit user approval)
- 📋 Version-tagged (ruleset version included)
- 📋 Reproducible (same data = same export)
- 📋 FHIR-compatible (future)

**Status**: **WAITING FOR PHASE C & D**

---

### **PHASE G: UNREAL MIND PALACE** 📋 **FUTURE**

**Goal**: Visualization-only client (does NOT become core logic).

#### **Critical Architecture Decision**:
> **Unreal is a VISUALIZATION CLIENT, NOT the core engine.**

#### **Implementation**:
```
Unreal Engine Client:
├── Subscribes to state snapshots
├── Visualizes: Operator indices, Overload, Decision risk
├── Deterministic rendering
└── Stateless (can crash without affecting system)
```

#### **Requirements**:
- 📋 Deterministic visualization
- 📋 Stateless rendering
- 📋 Recoverable on crash
- 📋 System stability remains intact if Unreal crashes

**Status**: **AFTER STABILITY** (Phase H)

---

### **PHASE H: PRODUCT IDENTITY CONSOLIDATION** 📋 **FUTURE**

**Goal**: Simplify branding and naming.

#### **Current Names**:
- ❌ AutoPC (deprecated)
- ❌ Lullaby (transitional)
- ✅ HecateonCore (platform)
- ✅ NYPHOS (module)
- ✅ Prometheon (module)

#### **Recommended Final Structure**:
```
Platform: HecateonCore
Desktop Client: Hecateon Desktop
Web Client: Hecateon Dashboard
Modules:
├── NYPHOS (Stability)
└── Prometheon (Cognition)
```

#### **Actions**:
- 📋 Remove "Lullaby" from architecture layer
- 📋 Update all branding to "Hecateon"
- 📋 Finalize marketing materials

**Status**: **AFTER TECHNICAL COMPLETION**

---

## 🚀 IMMEDIATE NEXT STEPS

### **NOW (Phase C - Week 1)**:
1. ✅ Create `Modules/Prometheon/` directory structure
2. ✅ Define core Prometheon models
3. ✅ Build `PrometheronEngine.cs` skeleton
4. ✅ Create Prometheon event store

### **Next Week (Phase C - Week 2)**:
1. Implement Transactional Analyzer
2. Build Narrative Reframer
3. Create Foresight Simulator
4. Unit tests for all components

### **Week 3-4 (Phase C Complete)**:
1. Desktop UI integration
2. Integration tests with NYPHOS
3. Determinism validation
4. Documentation complete

### **Phase D (Month 2)**:
1. Unified safety ladder
2. Combined risk assessment
3. Crisis UI enhancement

---

## 📋 TECHNICAL CHECKLIST

### **Phase C: Prometheon Integration**

- [ ] Create `Modules/Prometheon/` directory
- [ ] Define `OperatorState.cs` model
- [ ] Define `Transaction.cs` model
- [ ] Define `NarrativeFrame.cs` model
- [ ] Define `ForesightSimulation.cs` model
- [ ] Create `PrometheronEngine.cs`
- [ ] Create `TransactionalAnalyzer.cs`
- [ ] Create `NarrativeReframer.cs`
- [ ] Create `ForesightSimulator.cs`
- [ ] Create `OperatorChat.cs`
- [ ] Build Prometheon event store
- [ ] Add to Desktop UI
- [ ] Unit tests (100% coverage)
- [ ] Integration tests with NYPHOS
- [ ] Determinism verification
- [ ] Hysteresis validation
- [ ] Documentation complete

---

## 🎯 SUCCESS CRITERIA

### **Phase C Complete When**:
- ✅ All Prometheon models defined
- ✅ PrometheronEngine running locally
- ✅ Events are append-only
- ✅ All logic is deterministic
- ✅ Desktop UI integrated
- ✅ Tests pass at 100%
- ✅ Documentation complete
- ✅ No breaking changes to NYPHOS

---

## 📊 PROJECT METRICS

### **Current State**:
- **Architecture**: ✅ Solidified
- **NYPHOS Module**: ✅ Complete
- **Prometheon Module**: 📋 0% (Starting now)
- **Safety Systems**: ✅ 70% (NYPHOS only)
- **Remote Access**: ❌ 0% (Planned)
- **Clinical Export**: 📋 30% (Structure exists)

### **Target (End of Phase D)**:
- **Architecture**: ✅ 100%
- **NYPHOS Module**: ✅ 100%
- **Prometheon Module**: ✅ 100%
- **Safety Systems**: ✅ 100%
- **Remote Access**: 📋 30%
- **Clinical Export**: ✅ 80%

---

## 🔥 CRITICAL SUCCESS FACTORS

1. **Desktop-first always** - Never make web the authority
2. **Append-only always** - Never mutate historical data
3. **Deterministic always** - Same input = same output
4. **Consent always** - User approves everything
5. **No AI magic** - All outputs explainable

---

## 📝 DECISION LOG

### **2025-01-XX: Repository Migration**
- **Decision**: Migrate from AutoPC to HecateonCore
- **Rationale**: Unified branding, consolidated development
- **Impact**: All future work in single repo

### **2025-01-XX: Desktop-First Architecture**
- **Decision**: Desktop is primary, web is secondary
- **Rationale**: Local control, privacy-first
- **Impact**: Engines run locally, web consumes state

### **2025-01-XX: Module Structure**
- **Decision**: NYPHOS + Prometheon as modules inside platform
- **Rationale**: Separation of concerns, maintainability
- **Impact**: Clean architecture, independent testing

---

**Last Updated**: January 2025  
**Next Review**: After Phase C completion

---

*For detailed architecture, see `HECATEONCORE_PROJECT_OVERVIEW.md`*
