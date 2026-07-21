# Sample Lab release assets

Assets for the Uno Platform 6.6 sample lab. One per capability, plus the two
proof assets that need more than a section screenshot.

Capture method is recorded per asset because it determines what the asset can
honestly claim. `uno-app MCP` means a native-resolution element screenshot of
the live visual tree. `frame sequence` means PrintWindow frames assembled with
ffmpeg, used only where motion is the proof.

| # | Asset | Shows | Method | Status |
|---|---|---|---|---|
| 01 | Accessibility section | Form specimen driving name/role/value/state | uno-app MCP | captured |
| 02 | **UIA tree** | 70-element live automation tree read out-of-process | UIA walker + window capture | captured |
| 03 | Scroll anchoring section | VerticalAnchorRatio picker, live CurrentAnchor readout | uno-app MCP | captured |
| 04 | **Scroll anchoring GIF** | Items inserting above while the anchored row holds, split against anchoring off | frame sequence | in progress |
| 05 | Input & IME section | Composed input and complex scripts reaching a TextBox | uno-app MCP | captured |
| 06 | Text features section | Spell check, highlighters, trimming, fallback | uno-app MCP | captured |
| 07 | **Font fallback card** | Latin + emoji + CJK in one run, no tofu | uno-app MCP | captured |
| 08 | Element theming section | A theme applied to a subtree, not the window | uno-app MCP | captured |
| 09 | Vector graphics section | Caps, dashes, joins, Geometry.Transform | uno-app MCP | captured |
| 10 | Projection section | PlaneProjection and Matrix3DProjection, layout slot holding | uno-app MCP | captured |
| 11 | Menus & context section | Nested submenus, accelerators, context events | uno-app MCP | captured |
| 12 | Sample index | The eight-card home grid | uno-app MCP | captured |

## Notes per asset

**02 · UIA tree.** The strongest available proof that a Skia-rendered UI is
readable by assistive tech, because it is read by an out-of-process UIA client,
the same surface a screen reader consumes. Taken from a Release build launched
as a bare exe so no devserver or Hot Design nodes appear. The tree is also
drivable: `InvokePattern.Invoke()` on a nav item moves the breadcrumb.

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
