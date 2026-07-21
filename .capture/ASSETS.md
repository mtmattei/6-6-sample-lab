# Sample Lab release assets

> Numbering note: only ASSET 03 (UIA tree) and ASSET 20 (WebGL pair) carry
> numbers from the real DevRel asset list. Everything else is numbered locally
> and should be reconciled against that list before use.

Assets for the Uno Platform 6.6 sample lab. One per capability, plus the two
proof assets that need more than a section screenshot.

Capture method is recorded per asset because it determines what the asset can
honestly claim. `uno-app MCP` means a native-resolution element screenshot of
the live visual tree. `frame sequence` means PrintWindow frames assembled with
ffmpeg, used only where motion is the proof.

| # | Asset | Shows | Method | Status |
|---|---|---|---|---|
| 01 | Accessibility section | Form specimen driving name/role/value/state | uno-app MCP | captured |
| 03 | **UIA tree** (real ASSET 03) | 70-element live automation tree read out-of-process | UIA walker + window capture | text captured; tool screenshot blocked on install |
| 03 | Scroll anchoring section | VerticalAnchorRatio picker, live CurrentAnchor readout | uno-app MCP | captured |
| 04 | **Scroll anchoring GIF** | Items inserting above while the anchored row holds, split against anchoring off | frame sequence | not started |
| 20 | **WebGL pair** (real ASSET 20) | Blank on 6.5 vs full UI on 6.6, WebGL disabled | — | not producible, see notes |
| 05 | Input & IME section | Composed input and complex scripts reaching a TextBox | uno-app MCP | captured |
| 06 | Text features section | Spell check, highlighters, trimming, fallback | uno-app MCP | captured |
| 07 | **Font fallback card** | Latin + emoji + CJK in one run, no tofu | uno-app MCP | captured |
| 08 | Element theming section | A theme applied to a subtree, not the window | uno-app MCP | captured |
| 09 | Vector graphics section | Caps, dashes, joins, Geometry.Transform | uno-app MCP | captured |
| 10 | Projection section | PlaneProjection and Matrix3DProjection, layout slot holding | uno-app MCP | captured |
| 11 | Menus & context section | Nested submenus, accelerators, context events | uno-app MCP | captured |
| 12 | Sample index | The eight-card home grid | uno-app MCP | captured |

## Notes per asset

**03 · UIA tree.** The strongest available proof that a Skia-rendered UI is
readable by assistive tech, because it is read by an out-of-process UIA client,
the same surface a screen reader consumes. Taken from a Release build launched
as a bare exe so no devserver or Hot Design nodes appear. The tree is also
drivable: `InvokePattern.Invoke()` on a nav item moves the breadcrumb.

Incomplete against the brief, which asks for a *screenshot* of Accessibility
Insights or a UI-tree viewer. What exists is the tree as text from an
equivalent out-of-process read. Windows ships no UIA tree viewer, and
Accessibility Insights is a per-machine MSI needing elevation, so the tool
screenshot is blocked on a UAC prompt. Installer staged, signature verified as
Microsoft Corporation, v1.1.2924.01.

**20 · WebGL pair.** Not producible as briefed. The automatic
WebGL-to-software fallback shipped in 6.5, not 6.6: `BrowserRenderer.cs` is
absent at tag 6.4.242, present at 6.5.64, and byte-identical between 6.5.237
and 6.6.166. A 6.5 build will not show the blank page the pair needs.

**04 · Scroll anchoring GIF.** The only asset where a still cannot carry the
claim. Anchoring is defined by what does *not* move, so it needs the
before/after of an insert. Split screen against anchoring off makes the
contrast self-evident rather than asserted.

**07 · Font fallback card.** Verified at 3x that every script resolves to real
glyphs, colour emoji included and Arabic correctly joined. Native capture at
660x188; the app renders at scale 1 and the Windows Skia host ignores
`UNO_DISPLAY_SCALE_OVERRIDE`, so anything larger is either a display-scale
change or upscaling.

**10 · Projection section.** Recaptured after the Question annotations were
removed. FIG.16 drives rotation and M21 skew rather than M34 perspective,
because M34 measurably does nothing on Skia 6.6.
