# Display font

The headings and brand use a self-hosted Noto Sans SC font, selected for compact Chinese lettering and consistent rendering across Windows and other browser hosts.

- Upstream: https://github.com/notofonts/noto-cjk
- Source file: existing `C:/Windows/Fonts/NotoSansSC-VF.ttf` (Noto Sans SC variable font); source metadata verified during generation.
- License: SIL Open Font License 1.1, included in `OFL.txt` from https://raw.githubusercontent.com/google/fonts/main/ofl/notosanssc/OFL.txt
- Local transformation: fontTools instantiated weight 650; WOFF2 subset includes GB2312 characters, ASCII, CJK punctuation and current UI text. Uncommon account-name characters can use the declared fallback.
- Body copy keeps the platform Chinese text stack; measured times retain monospace/tabular numerals.

No remote font request is needed at runtime. The subset font and license must be distributed together.
