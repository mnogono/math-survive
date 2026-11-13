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
  // Символов вне шрифтов MathJax (например, кириллицы в \text{}) в SVG нет как контуров; Godot <text> не рисует.
  if (svg.includes('<text')) throw new Error(`Символы без контуров (кириллица?) в TeX: ${tex}`);
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

// prompt_image: готовый SVG-рисунок условия, копируется как есть по тому же относительному пути images/<file>.svg.
const srcDir = path.dirname(srcPath);
const imagesDir = path.join(outDir, 'images');
if (fs.existsSync(imagesDir))
  for (const f of fs.readdirSync(imagesDir)) if (f.endsWith('.svg')) fs.rmSync(path.join(imagesDir, f));

function isImagePath(p) {
  return typeof p === 'string' && p.startsWith('images/') && p.toLowerCase().endsWith('.svg')
    && p.length > 'images/.svg'.length && !p.includes('\\') && !p.includes(':')
    && p.split('/').every((s) => s && s !== '.' && s !== '..');
}

const unescapeXml = (s) => s.replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&amp;/g, '&');
const attr = (attrs, name) => attrs.match(new RegExp(`\\b${name}="([^"]*)"`))?.[1];

// Godot (ThorVG) не рисует <text>, поэтому каждая надпись рисунка — TeX, превращённый MathJax в контуры.
// <text x y [font-size=20] [text-anchor=start|middle|end] [fill]>TeX</text>; (x, y) — точка базовой линии.
function labelToPaths(attrs, tex) {
  const node = doc.convert(unescapeXml(tex.trim()), { display: false });
  const svg = adaptor.innerHTML(node).match(/<svg[\s\S]*<\/svg>/)?.[0];
  if (!svg || svg.includes('data-mjx-error') || svg.includes('merror') || svg.includes('<text'))
    throw new Error(`Ошибка TeX в надписи рисунка: ${tex}`);
  const [minX, , width] = svg.match(/viewBox="([^"]+)"/)[1].split(/\s+/).map(Number);
  const inner = svg.replace(/^<svg[^>]*>/, '').replace(/<\/svg>$/, '').replace(/<defs>\s*<\/defs>/, '');
  const x = Number(attr(attrs, 'x') ?? 0);
  const y = Number(attr(attrs, 'y') ?? 0);
  const k = Number(attr(attrs, 'font-size') ?? 20) / 1000;
  const anchor = attr(attrs, 'text-anchor') ?? 'start';
  const shift = anchor === 'middle' ? width / 2 : anchor === 'end' ? width : 0;
  const fill = attr(attrs, 'fill') ?? INK;
  return `<g transform="translate(${x} ${y}) scale(${k}) translate(${-(minX + shift)} 0)" fill="${fill}" stroke="${fill}">`
    + inner.replace(/currentColor/g, fill) + '</g>';
}

function copyImage(id, rel) {
  if (!isImagePath(rel)) throw new Error(`${id}: prompt_image должен быть images/*.svg, получено: ${rel}`);
  const from = path.join(srcDir, rel);
  if (!fs.existsSync(from)) throw new Error(`${id}: нет файла рисунка ${from}`);
  const to = path.join(outDir, rel);
  fs.mkdirSync(path.dirname(to), { recursive: true });
  const svg = fs.readFileSync(from, 'utf8').replace(/<text\b([^>]*)>([\s\S]*?)<\/text>/g, (_, a, t) => labelToPaths(a, t));
  if (svg.includes('<text')) throw new Error(`${id}: в рисунке остался <text>`);
  fs.writeFileSync(to, svg);
}

const out = src.map((q) => {
  if (q.prompt_image !== undefined) copyImage(q.id, q.prompt_image);
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
