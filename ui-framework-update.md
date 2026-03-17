# UI Framework Update Plan: Next.js + shadcn/ui

## Overview

**Goal:** Replace the ASP.NET Core Razor Views frontend with a modern Next.js (React) app using shadcn/ui as the component library.

**Architecture after migration:**
- **Backend stays exactly the same** — ASP.NET Core 9, all `/api/*` endpoints, SignalR hub, PostgreSQL
- **Frontend:** New Next.js app (separate directory) that talks to the existing backend via JWT auth
- **Auth:** JWT token stored in an httpOnly cookie or localStorage, used in every API request

This approach means **zero risk to the backend** — you migrate incrementally and can run both apps side by side during development.

---

## Technology Stack

| Concern | Choice | Why |
|---|---|---|
| Framework | Next.js 15 (App Router) | Server components, file-based routing, streaming support |
| UI components | shadcn/ui | Copy-paste components, built on Radix UI, fully customizable |
| Styling | Tailwind CSS v4 | Utility classes, shadcn/ui requires it |
| Real-time | `@microsoft/signalr` | Same SignalR library already used in the current app |
| HTTP client | `fetch` (native) + custom hooks | No extra dependency needed |
| Auth state | `zustand` or React Context | Lightweight client-side JWT state |
| Charts | `recharts` | React-native, works seamlessly with shadcn |
| Forms | `react-hook-form` + `zod` | shadcn/ui forms use these |
| Icons | `lucide-react` | Included with shadcn/ui |

---

## Current Pages → New Routes Mapping

| Current Razor View | New Next.js Route | Notes |
|---|---|---|
| `Home/Index` | `/` (public) | Landing page |
| `Account/Login` | `/login` | JWT auth, Google OAuth button |
| `Account/Register` | `/register` | |
| `Discover/Index` | `/discover` | Public profiles grid |
| `Profile/Index` | `/settings` | Edit own profile |
| `Profile/Public` | `/u/[username]` | Public profile page |
| `Friends/Index` | `/friends` | Friend list + pending requests |
| `Chat/Index` | `/chat` + `/chat/[friendId]` | Split into list + conversation |
| `Nutrition/Index` | `/nutrition` | Dashboard with charts |
| `Nutrition/Create` | `/nutrition/new` | SSE streaming analyze |
| `Nutrition/Result` | `/nutrition/[id]` | Single plan detail |
| `Nutrition/WeeklyReview` | `/nutrition/weekly` | Weekly summary |
| `Challenge/Index` | `/challenges` | Active/pending/completed tabs |
| `Challenge/Create` | `/challenges/new` | Create challenge form |
| `Challenge/Dashboard` | `/challenges/[id]` | Leaderboard + real-time updates |
| `Challenge/Summary` | `/challenges/[id]/summary` | AI summary |

---

## Backend API — What Already Exists (No Changes Needed)

All these endpoints are already JWT-authenticated and return JSON. The Next.js app will consume them directly.

### Auth — `POST /api/auth/*`
- `POST /api/auth/login` → returns `{ token, userId, email, displayName }`
- `POST /api/auth/register` → same response
- `POST /api/auth/google` → Google `idToken` → JWT response

### Profile — `GET/PUT /api/profile`
- `GET /api/profile` → current user profile
- `PUT /api/profile` → update username, displayName, bio, isPublic

### Nutrition — `/api/nutrition`
- `GET /api/nutrition` → all plans + today summary + active challenge
- `GET /api/nutrition/{id}` → single plan
- `POST /api/nutrition/analyze` → **SSE streaming** (multipart/form-data, file + description)
- `DELETE /api/nutrition/{id}`

### Challenges — `/api/challenges`
- `GET /api/challenges` → active/pending/completed/community lists
- `POST /api/challenges` → create
- `GET /api/challenges/{id}/leaderboard` → full leaderboard
- `POST /api/challenges/{id}/join`
- `POST /api/challenges/{id}/leave`
- `DELETE /api/challenges/{id}`

### Social — `/api/friends`
- `GET /api/friends` → friends list + pending received
- `POST /api/friends/request` → send request
- `POST /api/friends/accept`
- `POST /api/friends/decline`
- `POST /api/friends/unfriend`

### Chat — `/api/chat`
- `GET /api/chat/conversations` → conversation list with unread counts
- `GET /api/chat/{friendId}?skip=0&take=60` → paginated messages

### Discover — `/api/discover`
- `GET /api/discover` → public profiles with friendship status

### SignalR — `wss://.../chatHub`
Client → Server events: `SendMessage(receiverId, content)`, `MarkRead(friendId)`, `StartTyping(receiverId)`, `StopTyping(receiverId)`

Server → Client events: `ReceiveMessage`, `MessagesRead`, `TypingStarted`, `TypingStopped`, `UserOnline`, `UserOffline`, `ChallengeUpdate`

---

## Implementation Plan (Phases)

---

### Phase 1 — Project Setup

**Steps:**
1. Create a new Next.js app in a sibling directory: `web-app-dupi-next/`
   ```bash
   npx create-next-app@latest web-app-dupi-next --typescript --tailwind --app --src-dir
   ```
2. Install shadcn/ui and initialize it:
   ```bash
   npx shadcn@latest init
   ```
3. Install additional dependencies:
   ```bash
   npm install @microsoft/signalr recharts react-hook-form zod @hookform/resolvers zustand lucide-react
   ```
4. Add a `.env.local` file:
   ```
   NEXT_PUBLIC_API_URL=http://localhost:5119
   NEXT_PUBLIC_SIGNALR_URL=http://localhost:5119/chatHub
   ```
5. Create a shared API client utility (`src/lib/api.ts`) that:
   - Reads JWT from localStorage
   - Attaches `Authorization: Bearer <token>` to every request
   - Handles 401 → redirect to `/login`

**Deliverable:** Blank Next.js app running on `localhost:3000`, able to call the ASP.NET API.

---

### Phase 2 — Auth

**Pages:** `/login`, `/register`

**Steps:**
1. Install shadcn components: `Button`, `Input`, `Card`, `Form`, `Label`
2. Build `/login`:
   - Email + password form using `react-hook-form` + `zod`
   - On submit → `POST /api/auth/login` → store JWT + userId in zustand + localStorage
   - Google Sign-In button → use Google Identity Services JS SDK → send `idToken` to `POST /api/auth/google`
3. Build `/register`:
   - Same pattern
4. Create an auth context/store (`src/store/auth.ts`) with:
   - `token`, `userId`, `displayName`
   - `login(token, user)` and `logout()` actions
5. Create a `useAuth` hook and a `<ProtectedRoute>` component that redirects to `/login` if not authenticated

**Deliverable:** Working login/register that stores a JWT and redirects to `/nutrition`.

---

### Phase 3 — Layout + Navigation

**Steps:**
1. Install shadcn components: `Tooltip`, `Avatar`, `DropdownMenu`, `Badge`, `Separator`
2. Build the main app layout (`src/app/(app)/layout.tsx`):
   - Collapsible sidebar (mirrors the current one) using Tailwind + `useState`
   - Nav items: Discover, Nutrition, Friends (with unread badge), Chat (with unread badge), Challenges
   - Bottom user menu: avatar initials, dropdown with Settings + Sign out
3. Create a public layout (`src/app/(public)/layout.tsx`) for pages that don't need auth (landing, login, register, public profiles)
4. Add active route highlighting using Next.js `usePathname()`

**Deliverable:** App shell with sidebar that works on desktop and mobile.

---

### Phase 4 — Nutrition Module

This is the most complex module because of SSE streaming. Do it early to derisk it.

**Pages:** `/nutrition`, `/nutrition/new`, `/nutrition/[id]`, `/nutrition/weekly`

**Steps:**
1. Install shadcn components: `Card`, `Progress`, `Tabs`, `Badge`, `Skeleton`
   Install chart library: `recharts`
2. Build `/nutrition` (dashboard):
   - Today summary card (calories, macros, streak badge)
   - Recharts `LineChart` for score over time, `BarChart` for calories and macros
   - Meal cards grouped by date, with score badge, meal type tag, delete button
3. Build `/nutrition/new` (analyze):
   - Form with title, meal type selector, text description, file upload (drag-and-drop)
   - On submit → `POST /api/nutrition/analyze` with `multipart/form-data`
   - **SSE streaming:** consume with `EventSource` or `fetch` + `ReadableStream`
   - Show "thinking" spinner while streaming, then progressively render the output text
   - On `type: "done"` → redirect to `/nutrition/[id]`
4. Build `/nutrition/[id]` (result):
   - Full analysis card: food description, calorie range, macro breakdown, score, what's good, what to improve
   - Macro donut chart
5. Build `/nutrition/weekly`:
   - Weekly summary of all meals

**Note on SSE:** Use `fetch()` with `response.body.getReader()` — this works in Next.js client components and gives full control over the stream.

**Deliverable:** Full nutrition flow including live-streaming AI analysis.

---

### Phase 5 — Chat Module

**Pages:** `/chat`, `/chat/[friendId]`

**Steps:**
1. Install shadcn components: `ScrollArea`, `Separator`, `Textarea`
2. Create a SignalR hook (`src/hooks/useSignalR.ts`):
   - Connects to `/chatHub` with the JWT token in the query string or header
   - Exposes `sendMessage`, `markRead`, `startTyping`, `stopTyping`
   - Fires callbacks for `ReceiveMessage`, `TypingStarted`, `TypingStopped`, `UserOnline`, `UserOffline`, `ChallengeUpdate`
   - Manages connection lifecycle (auto-reconnect)
3. Build `/chat` (conversation list):
   - List of friends with last message preview and unread count badge
   - Click → navigate to `/chat/[friendId]`
   - Real-time updates: when `ReceiveMessage` arrives, update the conversation list
4. Build `/chat/[friendId]` (conversation):
   - Load messages from `GET /api/chat/{friendId}`
   - Message bubbles grouped by sender/time (same cluster logic as current app)
   - Support for challenge share cards (detect `[challenge:id]` format in messages)
   - Typing indicator (show when `TypingStarted` received)
   - Online presence dot next to name
   - Message input → `sendMessage` via SignalR
   - `MarkRead` when conversation is opened/focused
   - Scroll to bottom on new message
   - Infinite scroll upward to load older messages

**Deliverable:** Fully functional real-time chat with typing indicators and presence.

---

### Phase 6 — Challenges Module

**Pages:** `/challenges`, `/challenges/new`, `/challenges/[id]`, `/challenges/[id]/summary`

**Steps:**
1. Install shadcn components: `Dialog`, `Select`, `Tabs`, `Table`, `Alert`
2. Build `/challenges` (index):
   - Tabs: Active, Pending Invites, Completed, Community
   - Challenge cards with metric, target, participant count, status badge
   - Accept/decline buttons on pending invites
3. Build `/challenges/new` (create):
   - Form: title, description, metric (Score/Calories/Proteins/etc.), target value, direction (AtLeast/AtMost), type (Solo/FriendChallenge), invite friends (multi-select from friends list)
4. Build `/challenges/[id]` (dashboard):
   - My progress bar chart (7-day breakdown with green/red bars)
   - Leaderboard table with rank medals, daily dot grid, days hit
   - Real-time updates: subscribe to `ChallengeUpdate` SignalR event → refresh leaderboard (use React Query or manual refetch, no `location.reload()`)
   - Join/Leave/Delete actions
5. Build `/challenges/[id]/summary` (AI summary):
   - Trigger and display the AI-generated challenge summary

**Deliverable:** Full challenges flow with live leaderboard updates.

---

### Phase 7 — Social (Friends + Discover)

**Pages:** `/friends`, `/discover`, `/u/[username]`

**Steps:**
1. Build `/friends`:
   - Two sections: Friends list + Pending requests
   - Friend card with avatar initials, display name, online indicator (from SignalR)
   - Actions: Chat button, Unfriend button
   - Pending section: Accept / Decline buttons
2. Build `/discover`:
   - Grid of public user profile cards
   - Friendship status badge (none / pending_sent / friends)
   - Add Friend button → `POST /api/friends/request`
3. Build `/u/[username]` (public profile):
   - Public-facing profile page (no auth required)
   - Display name, bio, public status

**Deliverable:** Full social graph UI.

---

### Phase 8 — Profile Settings

**Page:** `/settings`

**Steps:**
1. Build `/settings`:
   - Form with username, display name, bio, public toggle
   - Validate username format client-side (3-30 chars, alphanumeric/hyphens/underscores)
   - `PUT /api/profile` on submit
   - Show current email (read-only)
   - Success/error toast notifications using shadcn `Sonner` (toast library)

**Deliverable:** Working profile settings page.

---

### Phase 9 — Home Page + Polish

**Steps:**
1. Build `/` (landing):
   - Hero section: headline, sub-copy, CTA buttons
   - Feature cards (3 columns)
   - If authenticated → redirect to `/nutrition`
2. Global polish:
   - Loading skeletons for every page (shadcn `Skeleton`)
   - Empty states for all lists
   - Error boundaries
   - Mobile responsiveness review
   - Favicon, page titles (`metadata` API in Next.js)
   - Dark mode support (shadcn/ui supports this out of the box via CSS variables)

---

### Phase 10 — Cutover

**Steps:**
1. Update CORS in `Program.cs` to allow `http://localhost:3000` and the production Next.js domain
2. Configure SignalR to accept JWT from query string (needed for WebSocket connections):
   In `Program.cs`, the `OnMessageReceived` event already likely handles this — verify it works from Next.js
3. Deploy Next.js app (Vercel is the easiest choice, free tier available)
4. Point your domain to the Next.js app
5. The ASP.NET app continues running as a pure API — you can remove the Razor Views folder entirely once the migration is confirmed working

---

## Key Technical Considerations

### SSE Streaming (Nutrition Analyze)
The `POST /api/nutrition/analyze` endpoint uses Server-Sent Events. In Next.js:
```typescript
const response = await fetch(`${API_URL}/api/nutrition/analyze`, {
  method: 'POST',
  headers: { Authorization: `Bearer ${token}` },
  body: formData,
});
const reader = response.body!.getReader();
const decoder = new TextDecoder();
// read chunks and parse "data: {...}\n\n" format
```
This is straightforward in a client component.

### SignalR JWT Auth
SignalR WebSockets can't send custom headers, so the JWT must be passed via query string:
```typescript
new HubConnectionBuilder()
  .withUrl('/chatHub', {
    accessTokenFactory: () => getToken()
  })
  .build()
```
The ASP.NET backend already supports this pattern (the `OnMessageReceived` JWT event in `Program.cs` handles extracting the token from query string).

### Google OAuth
Use the official `@react-oauth/google` library. On login success, send the `credential` (idToken) to `POST /api/auth/google`.

### CORS
Add the Next.js origin to the allowed origins in `Program.cs` before starting development.

---

## Suggested shadcn/ui Components to Install Upfront

```bash
npx shadcn@latest add button card input label form textarea
npx shadcn@latest add badge avatar dropdown-menu separator tooltip
npx shadcn@latest add tabs dialog select scroll-area table progress skeleton
npx shadcn@latest add alert sonner
```

---

## File Structure (Next.js App)

```
web-app-dupi-next/
├── src/
│   ├── app/
│   │   ├── (public)/           # No auth required
│   │   │   ├── layout.tsx
│   │   │   ├── page.tsx        # /  (landing)
│   │   │   ├── login/page.tsx
│   │   │   ├── register/page.tsx
│   │   │   └── u/[username]/page.tsx
│   │   └── (app)/              # Auth required
│   │       ├── layout.tsx      # Sidebar + auth guard
│   │       ├── nutrition/
│   │       ├── challenges/
│   │       ├── friends/
│   │       ├── chat/
│   │       ├── discover/
│   │       └── settings/
│   ├── components/
│   │   ├── ui/                 # shadcn/ui components (auto-generated)
│   │   ├── layout/             # Sidebar, NavItem, UserMenu
│   │   ├── chat/               # MessageBubble, ConversationItem, TypingIndicator
│   │   ├── nutrition/          # MealCard, MacroBar, StreamingAnalysis
│   │   └── challenges/         # ChallengeCard, Leaderboard, DayProgress
│   ├── hooks/
│   │   ├── useSignalR.ts       # SignalR connection lifecycle
│   │   ├── useAuth.ts          # Auth state
│   │   └── useApi.ts           # Typed fetch wrapper
│   ├── lib/
│   │   ├── api.ts              # Base fetch with JWT injection
│   │   └── utils.ts            # shadcn cn() helper + misc
│   └── store/
│       └── auth.ts             # Zustand store for auth state
├── .env.local
└── package.json
```

---

## What This Will Look Like

With shadcn/ui you get:
- Clean, minimal design system with consistent spacing and typography
- Proper dark mode support
- Accessible components (keyboard navigation, screen readers)
- Smooth animations and transitions
- A modern feel similar to Linear, Vercel, Notion

The overall look will go from "Bootstrap CRUD app" to something that looks like a real product.
