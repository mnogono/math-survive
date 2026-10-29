// node build-pack.mjs <src.json> <outDir>
// Превращает поля prompt_tex / tex в SVG-файлы и пишет questions.json в формате пакета.
import fs from 'node:fs';
import path from 'node:path';
import { mathjax } from 'mathjax-full/js/mathjax.js';
import { TeX } from 'mathjax-full/js/input/tex.js';
import { SVG } from 'mathjax-full/js/output/svg.js';
import { liteAdaptor } from 'mathjax-full/js/adaptors/liteAdaptor.js';
import { RegisterHTMLHandler } from 'mathjax-full/js/handlers/html.js';
import { AllPackages } from 'mathjax-full/js/input/tex/AllPackages.js';

const EX_PX = 8;
const INK = '#1a1a1a';

const [srcPath, outDir] = process.argv.slice(2);
if (!srcPath || !outDir) {
  console.error('Usage: node build-pack.mjs <src.json> <outDir>');
  process.exit(1);
}

const adaptor = liteAdaptor();
RegisterHTMLHandler(adaptor);
const doc = mathjax.document('', {
  InputJax: new TeX({ packages: AllPackages }),
  OutputJax: new SVG({ fontCache: 'none' }),
});

function texToSvg(tex) {
  const node = doc.convert(tex, { display: true });
  let svg = adaptor.innerHTML(node); // содержимое mjx-container: один <svg>
  const m = svg.match(/<svg[\s\S]*<\/svg>/);
  if (!m) throw new Error(`MathJax не вернул svg для: ${tex}`);
  svg = m[0];
  if (svg.includes('data-mjx-error') || svg.includes('merror'))
    throw new Error(`Ошибка TeX: ${tex}`);
  svg = svg.replace(/(width|height)="([0-9.]+)ex"/g, (_, a, v) => `${a}="${+(parseFloat(v) * EX_PX).toFixed(2)}px"`);
  svg = svg.replace(/currentColor/g, INK);
  if (!svg.includes('xmlns=')) svg = svg.replace('<svg', '<svg xmlns="http://www.w3.org/2000/svg"');
  return svg;
}

const src = JSON.parse(fs.readFileSync(srcPath, 'utf8'));
const svgDir = path.join(outDir, 'svg');
fs.mkdirSync(svgDir, { recursive: true });
for (const f of fs.readdirSync(svgDir)) if (f.endsWith('.svg')) fs.rmSync(path.join(svgDir, f));

function writeSvg(name, tex) {
  fs.writeFileSync(path.join(svgDir, name), texToSvg(tex) + '\n');
  return `svg/${name}`;
}

const out = src.map((q) => {
  const { prompt_tex, options, ...rest } = q;
  const res = { ...rest };
  if (prompt_tex !== undefined) res.prompt_svg = writeSvg(`${q.id}-q.svg`, prompt_tex);
  res.options = options.map((o, k) => {
    const { tex, ...orest } = o;
    return tex !== undefined ? { ...orest, svg: writeSvg(`${q.id}-a${k}.svg`, tex) } : orest;
  });
  return res;
});

fs.writeFileSync(path.join(outDir, 'questions.json'), JSON.stringify(out, null, 2) + '\n');
console.log(`Записано вопросов: ${out.length}`);
