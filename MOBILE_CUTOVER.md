# Mobile cutover guide — Brewly (Expo) → GrabCoffee API (.NET)

Goal: the mobile app talks **only** to the .NET API. No `@supabase/supabase-js`
imports anywhere. Supabase itself stays (Postgres, GoTrue, Storage, edge
function) — only the *client library* goes away.

> Status: catalog reads (`stores`, `coffees`, `search`) are already cut over
> on the mobile branch `api-cutover/catalog`. Everything below builds on that.

Base URL: `https://grabcoffee-production.up.railway.app`
(desktop dev: `http://localhost:5000`. Scalar docs: `{base}/scalar/v1`.)

---

## 0. Principles

1. **Backend owns all trust.** Pricing, ownership, roles, status transitions
   live in the API. The app never computes totals it expects to be honored.
2. **One phase = one PR, always shippable.** Each phase below keeps the app
   working with both old and new code paths until its flag is removed.
3. **supabase-js dies last.** Delete the dependency only in Phase F after
   every screen is migrated and green.
4. **Realtime and Storage go direct until replaced.** They don't need
   supabase-js *writes through RLS* — see Phases D/E for the end state.

## 1. Transport + token store (do first, changes no features)

Extend `src/services/api.ts` (created in the catalog cutover):

- `EXPO_PUBLIC_API_URL` base URL (already there).
- Token storage in SecureStore (same adapter pattern as today):
  `grabcoffee.tokens` = `{ accessToken, refreshToken }`.
- `apiFetch`: attach `Authorization: Bearer <access>` when present.
- **401 handling:** single-flight `POST /auth/refresh` with the stored
  refresh token → store the **new pair** (refresh tokens rotate; reusing the
  old one 401s) → retry once. Refresh fails → clear tokens → route to sign-in.
- Errors: the API returns RFC-9457 `ProblemDetails`
  (`{ title, detail, status }`). Throw `Error(detail ?? title)` — same toast
  convention as today.

## 2. Auth rewrite (replaces `authStore` + supabase.auth)

| Today (supabase-js) | After (API) |
|---|---|
| `signInWithPassword` | `POST /auth/login` → store pair → `GET /auth/me` |
| `signUp` | `POST /auth/register` → store pair if returned, else "check inbox" when `emailConfirmationRequired` |
| `refreshSession` / `getSession` on launch | Stored refresh token → `POST /auth/refresh` once → `GET /auth/me`; 401 → clear → sign-in |
| `signOut` | Best-effort `POST /auth/logout` → clear storage (same as today) |
| `resetPasswordForEmail` | `POST /auth/forgot-password` (always silent, same as today) |
| `updateUser({ password })` | `POST /auth/change-password` (Bearer required) |
| `onAuthStateChange` | Delete. Session state = tokens in store + `GET /auth/me` profile |
| `authStore.session/profile` | `{ accessToken, refreshToken, profile }` where profile comes from `GET /auth/me` (`{ id, email, fullName, role }`) |

**Deep links without supabase-js.** Confirmation and recovery emails are still
sent by GoTrue and still deep-link back into the app — but the links carry
`access_token`/`refresh_token` in the URL fragment. Parse them out of the
link manually (`Linking.parse`), store the pair, discard the fragment. No
supabase-js needed:
- `sign-up` confirmation → link → store pair → signed in.
- `forgot-password` recovery → link → store pair → force `change-password`
  screen (same `isPasswordRecovery` flag pattern as today's `authStore`).

**Roles.** `me.role` is `seller | customer | driver` from `profiles` — same
values the app already switches on. No mapping changes.

## 3. Reads (catalog done; finish the rest)

| Mobile function(s) | Endpoint |
|---|---|
| `fetchActivePromotions`, `lookupPromoCode` | `GET /promotions?storeId=`, `GET /promotions/lookup?storeId=&code=` |
| `fetchFavoriteCoffees`, `fetchFavoriteStores`, favorite-id sets | `GET /favorites/coffees`, `GET /favorites/stores` (derive id sets client-side) |
| `fetchMyLoyaltyCards`, `fetchCardForStore` | `GET /loyalty/cards` (filter client-side for one store) |
| `fetchAddresses` | `GET /addresses` |
| `fetchOrdersList`, `fetchMyPurchases(Page)`, `fetchOrderWithItems` | `GET /orders/mine?page=&pageSize=`, `GET /orders/{id}` |
| `fetchMyShopOrders(Page)` | `GET /stores/mine/orders?page=&pageSize=` |
| `fetchDriverOrders` | `GET /orders/deliveries` |
| `fetchCoffeeReviews`, `fetchReviewedCoffeeIds` | `GET /coffees/{id}/reviews?limit=`, `GET /orders/{id}/reviewed-coffees` |
| `fetchOrderMessages` | `GET /orders/{id}/messages?limit=&before=` |
| `fetchMyStore`, `fetchStoreById`, `fetchStores` | Done (catalog). Seller screens reuse `GET /stores/mine` |
| `fetchMyCoffees`, `fetchMyOptions`, `fetchMyPromotions`, `fetchSellerEarnings` | `GET /stores/mine/coffees`, `GET /stores/mine/options`, `GET /stores/mine/promotions`, seller earnings is **not yet in the API** — port `computeSellerEarnings` as `GET /sellers/earnings` (backend work item, ~30 lines) |
| `fetchDriverProfile`, `fetchAvailableDrivers` | `GET /drivers/me`, `GET /drivers/available` (seller token) |
| `searchCoffees`, `searchStores` | Done (catalog, combined `GET /search`) |

Response shape note: the API serializes **camelCase**; the cutover maps back
to the existing snake_case row types at the service boundary, so screens and
hooks don't change. Keep doing that per service.

## 4. Writes (the trust boundary — port faithfully, don't redesign)

| Mobile function(s) | Endpoint | Notes |
|---|---|---|
| `fetchExpectedCartPrices` | `POST /orders/price-check` | Same per-line `{unitPrice, fullPrice\|null}` contract |
| `placeOrder` | `POST /orders` → 201 `{id}` | **Do not send** `subtotal/tax/total/discount/deliveryFee` — the server recomputes. **Do send** per-line `unitPrice` (freshness check). Keep the reprice-before-checkout flow; error strings are verbatim so toasts don't change |
| `attachPayment`, `setPaymentVerified` | `POST /orders/{id}/payment`, `POST /orders/{id}/payment/verify` | Same guards |
| `cancelOrder`, `updateOrderStatus`, `assignDriver` | `POST /orders/{id}/cancel`, `PATCH /orders/{id}/status`, `POST /orders/{id}/assign-driver` | Same state machine |
| `sendOrderMessage` | `POST /orders/{id}/messages` → 201 | Same 1000-char normalize |
| `submitCoffeeReview` | `POST /reviews` | Same guards |
| `create/update/deleteAddress`, `setDefaultAddress` | `/addresses` CRUD + `POST /addresses/{id}/default` |  |
| favorite toggles (coffee + store) | `POST/DELETE /favorites/...` | Endpoints are idempotent; optimistic-update code stays as-is |
| `become_seller` flow (`becomeSeller` + `updateMyStore`) | `POST /stores/onboard` → 201, then `PATCH /stores/mine` | Same two-step order |
| seller menu/options/promos CRUD, `updateMyStore`, `toggleCoffeeActive`, `setOptionCategoryScoping` | `/stores/mine/...` routes | Ownership resolved server-side; drop client `storeId` params |
| `registerAsDriver`, `setDriverAvailability` | `POST /drivers/register`, `PATCH /drivers/me/availability` |  |
| `updateDisplayName`, `deleteAccount` | `PATCH /me`, `DELETE /me` | Mobile still deletes the avatar via Storage first, then calls `DELETE /me` |
| push token upsert / cleanup | `PUT /push-tokens`, `DELETE /push-tokens` | Same triggers (sign-in/out, toggle) |

## 5. Realtime (chat, order tracking, promo sync)

Today these are Supabase Realtime subscriptions. Two-stage exit:

- **Stage 1 (no backend work): polling.** Replace each channel with
  `refetchInterval` on the existing React Query hooks (chat 5s while open,
  tracking 10s, promos 60s). Ship the supabase-js removal on top of this.
- **Stage 2 (backend work item): SignalR.** Add a hub (`/hubs/orders`) that
  pushes `OrderUpdated` / `ChatMessageReceived`; the DB trigger that feeds
  the push edge function can fan out here too. Mobile swaps polling for hub
  events per screen.

Do not keep a hidden supabase-js import "just for realtime" — that defeats
the cutover. Polling first, sockets later.

## 6. Storage uploads (avatar, coffee images)

Today mobile uploads straight to Storage buckets. Without supabase-js the app
can't sign requests — so the backend gains two proxy endpoints (**work item**,
small):

- `POST /uploads/avatar` and `POST /uploads/coffee-image` (multipart).
- Backend forwards the bytes to Supabase Storage REST **with the caller's
  Bearer token**, so existing Storage RLS (own-folder) keeps enforcing.
  Returns the public URL, which mobile stores via the normal update calls.
- Mobile keeps its picker/compress UX; only the `supabase.storage.from().upload`
  lines change.

Alternative (not recommended): make the buckets public-write. Don't — spam
and billing risk.

## 7. Deleting supabase-js (last step, all-or-nothing)

Preconditions: Phases 1–6 merged, full jest suite green, dev-build smoke of
every tab.

1. Delete `src/services/supabase.ts`.
2. `npm uninstall @supabase/supabase-js`.
3. Remove `EXPO_PUBLIC_SUPABASE_URL` / `EXPO_PUBLIC_SUPABASE_ANON_KEY` from
   `.env`, `.env.example`, and EAS secrets. Keep `EXPO_PUBLIC_API_URL`.
4. Delete `src/types/database.ts` `Functions` block if unused (keep `Row`
   types — the service mappers still target those shapes).
5. `npm run typecheck`, `npm run lint`, `npx jest`, EAS preview build.

## 8. Test plan per phase

- Existing jest suites must stay green (mock `@/services/api` in
  `jest.setup.js` the way `@/services/supabase` is mocked — done for catalog).
- Dev-build smoke per migrated screen, airplane-mode checks where
  `assertOnline` guards exist.
- Orders phase: place → pay → verify → status walk on a throwaway account;
  confirm loyalty stamps and push notifications still fire (DB triggers).
- Auth phase: fresh install → sign-up → confirm link → login → kill app →
  relaunch (refresh path) → logout.

## 9. Rollback

Every phase is its own PR off `main`. Revert the PR — the replaced service
functions are pure swaps with identical signatures, so rollback is one revert
with no screen changes.
