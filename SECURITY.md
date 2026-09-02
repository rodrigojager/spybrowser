# Security and data handling

SpyBrowser profile directories can contain authenticated cookies, browser
history, local storage, downloaded files, and other sensitive session data.
They must not be committed, copied into logs, or shared between tenants.

- Keep the SpyBrowser root outside source control in production.
- Restrict filesystem permissions to the worker identity.
- Store proxy passwords in a secret manager or environment variable.
- Do not embed credentials in proxy URLs or identity manifests.
- Use a separate identity for each trust boundary.
- Back up profiles only with the same controls used for credentials.
- Dispose `SpyBrowserSession` so the browser closes and the profile lease is released.

The experimental WebGL and navigator overrides are intentionally labeled as
detectable. They are not security boundaries and should not be represented as
native browser behavior.
