type ExportRequest = {
  type: 'export-selection' | 'export-page' | 'cancel';
};

type JsonValue = string | number | boolean | null | JsonValue[] | { [key: string]: JsonValue };

type ExportNode = {
  id: string;
  name: string;
  type: string;
  visible?: boolean;
  locked?: boolean;
  x?: number;
  y?: number;
  width?: number;
  height?: number;
  rotation?: number;
  opacity?: number;
  layout?: { [key: string]: JsonValue };
  appearance?: { [key: string]: JsonValue };
  text?: { [key: string]: JsonValue };
  vector?: { [key: string]: JsonValue };
  children?: ExportNode[];
};

figma.showUI(__html__, { width: 520, height: 640, themeColors: true });

figma.ui.postMessage({
  type: 'ready',
  selectionCount: figma.currentPage.selection.length,
  pageName: figma.currentPage.name,
});

figma.on('selectionchange', () => {
  figma.ui.postMessage({
    type: 'selection-change',
    selectionCount: figma.currentPage.selection.length,
  });
});

figma.ui.onmessage = (msg: ExportRequest) => {
  if (msg.type === 'cancel') {
    figma.closePlugin();
    return;
  }

  const roots = msg.type === 'export-selection'
    ? figma.currentPage.selection
    : figma.currentPage.children;

  if (roots.length === 0) {
    figma.notify('Export할 노드가 없습니다.');
    figma.ui.postMessage({
      type: 'export-error',
      message: '선택된 노드가 없습니다. 노드를 선택하거나 Page Export를 사용하세요.',
    });
    return;
  }

  const payload = {
    schemaVersion: 1,
    generator: 'FigmaToXaml.JsonExport',
    exportedAt: new Date().toISOString(),
    source: msg.type === 'export-selection' ? 'selection' : 'page',
    page: {
      id: figma.currentPage.id,
      name: figma.currentPage.name,
    },
    nodes: roots.map(serializeNode),
  };

  figma.ui.postMessage({
    type: 'export-result',
    fileName: makeFileName(figma.currentPage.name, payload.source),
    json: JSON.stringify(payload, null, 2),
  });

  figma.notify(`JSON Export 완료: ${roots.length}개 루트 노드`);
};

function serializeNode(node: SceneNode): ExportNode {
  const exported: ExportNode = {
    id: node.id,
    name: node.name,
    type: node.type,
  };

  addCommonProperties(exported, node);
  addLayoutProperties(exported, node);
  addAppearanceProperties(exported, node);
  addTextProperties(exported, node);
  addVectorProperties(exported, node);

  if ('children' in node) {
    exported.children = node.children.map(serializeNode);
  }

  return exported;
}

function addCommonProperties(target: ExportNode, node: SceneNode): void {
  target.visible = node.visible;
  target.locked = node.locked;

  if ('x' in node) target.x = round(node.x);
  if ('y' in node) target.y = round(node.y);
  if ('width' in node) target.width = round(node.width);
  if ('height' in node) target.height = round(node.height);
  if ('rotation' in node) target.rotation = round(node.rotation);
  if ('opacity' in node) target.opacity = round(node.opacity);
}

function addLayoutProperties(target: ExportNode, node: SceneNode): void {
  const source = node as unknown as Record<string, unknown>;
  const layout: { [key: string]: JsonValue } = {};

  copyValue(layout, source, 'layoutMode');
  copyValue(layout, source, 'layoutWrap');
  copyValue(layout, source, 'primaryAxisSizingMode');
  copyValue(layout, source, 'counterAxisSizingMode');
  copyValue(layout, source, 'primaryAxisAlignItems');
  copyValue(layout, source, 'counterAxisAlignItems');
  copyNumber(layout, source, 'itemSpacing');
  copyNumber(layout, source, 'paddingLeft');
  copyNumber(layout, source, 'paddingRight');
  copyNumber(layout, source, 'paddingTop');
  copyNumber(layout, source, 'paddingBottom');
  copyValue(layout, source, 'layoutAlign');
  copyValue(layout, source, 'layoutGrow');
  copyValue(layout, source, 'layoutPositioning');

  if ('constraints' in node) {
    layout.constraints = serializePlainObject(node.constraints);
  }

  if (Object.keys(layout).length > 0) {
    target.layout = layout;
  }
}

function addAppearanceProperties(target: ExportNode, node: SceneNode): void {
  const source = node as unknown as Record<string, unknown>;
  const appearance: { [key: string]: JsonValue } = {};

  if ('fills' in node && node.fills !== figma.mixed) {
    appearance.fills = node.fills.map(serializePaint);
  }

  if ('strokes' in node) {
    appearance.strokes = node.strokes.map(serializePaint);
  }

  copyNumber(appearance, source, 'strokeWeight');
  copyValue(appearance, source, 'strokeAlign');
  copyNumber(appearance, source, 'cornerRadius');
  copyNumber(appearance, source, 'topLeftRadius');
  copyNumber(appearance, source, 'topRightRadius');
  copyNumber(appearance, source, 'bottomRightRadius');
  copyNumber(appearance, source, 'bottomLeftRadius');

  if ('effects' in node) {
    appearance.effects = node.effects.map(serializeEffect);
  }

  if (Object.keys(appearance).length > 0) {
    target.appearance = appearance;
  }
}

function addTextProperties(target: ExportNode, node: SceneNode): void {
  if (node.type !== 'TEXT') {
    return;
  }

  const text: { [key: string]: JsonValue } = {
    characters: node.characters,
    textAlignHorizontal: node.textAlignHorizontal,
    textAlignVertical: node.textAlignVertical,
    textAutoResize: node.textAutoResize,
  };

  if (node.paragraphIndent !== figma.mixed) {
    text.paragraphIndent = round(node.paragraphIndent);
  }

  if (node.paragraphSpacing !== figma.mixed) {
    text.paragraphSpacing = round(node.paragraphSpacing);
  }

  if (node.fontName !== figma.mixed) {
    text.fontName = serializePlainObject(node.fontName);
  }

  if (node.fontSize !== figma.mixed) {
    text.fontSize = round(node.fontSize);
  }

  if (node.lineHeight !== figma.mixed) {
    text.lineHeight = serializePlainObject(node.lineHeight);
  }

  if (node.letterSpacing !== figma.mixed) {
    text.letterSpacing = serializePlainObject(node.letterSpacing);
  }

  target.text = text;
}

function addVectorProperties(target: ExportNode, node: SceneNode): void {
  if (node.type !== 'VECTOR' && node.type !== 'LINE' && node.type !== 'POLYGON' && node.type !== 'STAR') {
    return;
  }

  const source = node as unknown as Record<string, unknown>;
  const vector: { [key: string]: JsonValue } = {};

  copyValue(vector, source, 'strokeCap');
  copyValue(vector, source, 'strokeJoin');
  copyNumberArray(vector, source, 'dashPattern');

  if (Object.keys(vector).length > 0) {
    target.vector = vector;
  }
}

function serializePaint(paint: Paint): JsonValue {
  const paintVisible = paint.visible === undefined ? true : paint.visible;
  const paintOpacity = paint.opacity === undefined ? 1 : paint.opacity;
  const paintBlendMode = paint.blendMode === undefined ? 'NORMAL' : paint.blendMode;

  const result: { [key: string]: JsonValue } = {
    type: paint.type,
    visible: paintVisible,
    opacity: round(paintOpacity),
    blendMode: paintBlendMode,
  };

  if (paint.type === 'SOLID') {
    result.color = serializeColor(paint.color, paintOpacity);
  }

  if (paint.type === 'GRADIENT_LINEAR' || paint.type === 'GRADIENT_RADIAL' || paint.type === 'GRADIENT_ANGULAR' || paint.type === 'GRADIENT_DIAMOND') {
    result.gradientStops = paint.gradientStops.map((stop) => ({
      position: round(stop.position),
      color: serializeColor(stop.color, stop.color.a),
    }));
    result.gradientTransform = paint.gradientTransform.map((row) => row.map(round));
  }

  if (paint.type === 'IMAGE') {
    result.scaleMode = paint.scaleMode;
    result.imageHash = paint.imageHash === undefined ? null : paint.imageHash;
  }

  return result;
}

function serializeEffect(effect: Effect): JsonValue {
  const result = serializePlainObject(effect) as { [key: string]: JsonValue };

  if ('color' in effect && effect.color) {
    result.color = serializeColor(effect.color, effect.color.a);
  }

  return result;
}

function serializeColor(color: RGB | RGBA, opacity = 1): JsonValue {
  const alpha = 'a' in color ? color.a : opacity;
  return {
    r: round(color.r),
    g: round(color.g),
    b: round(color.b),
    a: round(alpha),
    hex: rgbaToHex(color.r, color.g, color.b, alpha),
  };
}

function serializePlainObject(value: unknown): JsonValue {
  if (value === null || value === undefined) {
    return null;
  }

  if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean') {
    return typeof value === 'number' ? round(value) : value;
  }

  if (Array.isArray(value)) {
    return value.map(serializePlainObject);
  }

  if (typeof value === 'object') {
    const result: { [key: string]: JsonValue } = {};
    for (const [key, nestedValue] of Object.entries(value as Record<string, unknown>)) {
      result[key] = serializePlainObject(nestedValue);
    }
    return result;
  }

  return String(value);
}

function copyValue(target: { [key: string]: JsonValue }, source: Record<string, unknown>, key: string): void {
  const value = source[key];
  if (value !== undefined && value !== figma.mixed) {
    target[key] = serializePlainObject(value);
  }
}

function copyNumber(target: { [key: string]: JsonValue }, source: Record<string, unknown>, key: string): void {
  const value = source[key];
  if (typeof value === 'number') {
    target[key] = round(value);
  }
}

function copyNumberArray(target: { [key: string]: JsonValue }, source: Record<string, unknown>, key: string): void {
  const value = source[key];
  if (Array.isArray(value) && value.every((item) => typeof item === 'number')) {
    target[key] = value.map(round);
  }
}

function round(value: number): number {
  return Math.round(value * 1000) / 1000;
}

function rgbaToHex(r: number, g: number, b: number, a: number): string {
  const parts = [a, r, g, b].map((part) => {
    const hex = Math.round(part * 255).toString(16).padStart(2, '0');
    return hex.toUpperCase();
  });

  return `#${parts.join('')}`;
}

function makeFileName(pageName: string, source: string): string {
  const safePageName = pageName.replace(/[^a-z0-9가-힣_-]+/gi, '-').replace(/^-+|-+$/g, '');
  return `${safePageName || 'figma'}-${source}.json`;
}
