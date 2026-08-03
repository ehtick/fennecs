import fs from 'node:fs';
import path from 'node:path';
import matter from 'gray-matter';
import { UserConfig } from 'vitepress';

/*
 * Sidebar subdividers, applied on top of the sidebar that vitepress-sidebar generates.
 *
 * Inferred from structure:
 *   A directory without an index page (e.g. docs/Advanced) is organizational: it renders as a small
 *   labeled subdivider bearing its name, with its children hoisted into the parent level.
 *
 * Explicit, via frontmatter on any page:
 *   divider: Some Label   – inserts a labeled subdivider above this page's (or folder's) sidebar entry
 *   divider: true         – inserts a plain line subdivider instead
 *   flatten: true         – on a folder's index.md: hoists the folder's children into the parent level;
 *                           the folder's own index page is no longer listed
 *
 * Sibling lists that gained hoisted entries are re-sorted by frontmatter `order` (default 0); a divider
 * block sorts by its lowest-ordered child and keeps its children glued beneath it.
 * Divider entries are link-less sidebar items; styling lives in theme/custom.css (.sidebar-divider).
 */

type SidebarItem = {
  text?: string;
  link?: string;
  items?: SidebarItem[];
  [key: string]: unknown;
};

const frontmatterCache = new Map<string, Record<string, any>>();

// vitepress-sidebar resolves its document root against process.cwd(), so we do the same
function frontmatterFor(link?: string): Record<string, any> {
  if (!link) return {};
  let rel = link.replace(/^\//, '');
  if (rel.endsWith('/')) rel += 'index.md';
  if (!rel.endsWith('.md')) rel += '.md';
  const file = path.join(process.cwd(), rel);

  let data = frontmatterCache.get(file);
  if (!data) {
    try {
      data = matter(fs.readFileSync(file, 'utf-8')).data;
    } catch {
      data = {};
    }
    frontmatterCache.set(file, data);
  }
  return data;
}

// linked entries sort by their page's `order`; link-less groups by their lowest-ordered descendant
function effectiveOrder(item: SidebarItem): number {
  if (item.link) {
    const order = frontmatterFor(item.link).order;
    return typeof order === 'number' ? order : 0;
  }
  if (item.items?.length) return Math.min(...item.items.map(effectiveOrder));
  return 0;
}

function dividerItem(label: string | true): SidebarItem {
  return { text: `<span class="sidebar-divider">${label === true ? '' : label}</span>` };
}

// each block is an unbreakable run of items (a divider stays glued to its section) with a sort key
function flattenPass(items: SidebarItem[]): SidebarItem[] {
  let hoisted = false;
  const blocks: { order: number; items: SidebarItem[] }[] = [];

  for (const item of items) {
    if (item.items) item.items = flattenPass(item.items);

    if (item.items?.length && !item.link && item.text) {
      blocks.push({ order: effectiveOrder(item), items: [dividerItem(item.text), ...item.items] });
      hoisted = true;
    } else if (item.items?.length && frontmatterFor(item.link).flatten === true) {
      blocks.push(...item.items.map((child) => ({ order: effectiveOrder(child), items: [child] })));
      hoisted = true;
    } else {
      blocks.push({ order: effectiveOrder(item), items: [item] });
    }
  }

  if (hoisted) blocks.sort((a, b) => a.order - b.order);
  return blocks.flatMap((block) => block.items);
}

function dividerPass(items: SidebarItem[]): SidebarItem[] {
  const result: SidebarItem[] = [];

  for (const item of items) {
    const divider = frontmatterFor(item.link).divider;
    if (typeof divider === 'string' || divider === true) result.push(dividerItem(divider));

    if (item.items) item.items = dividerPass(item.items);
    result.push(item);
  }
  return result;
}

function decorate(items: SidebarItem[]): SidebarItem[] {
  return dividerPass(flattenPass(items));
}

export function withSidebarDividers<T extends UserConfig>(config: T): T {
  const sidebar = (config.themeConfig as any)?.sidebar;

  if (Array.isArray(sidebar)) {
    (config.themeConfig as any).sidebar = decorate(sidebar);
  } else if (sidebar && typeof sidebar === 'object') {
    // multi-path sidebar: { '/docs/': SidebarItem[] | { items: SidebarItem[] } }
    for (const key of Object.keys(sidebar)) {
      if (Array.isArray(sidebar[key])) sidebar[key] = decorate(sidebar[key]);
      else if (Array.isArray(sidebar[key]?.items)) sidebar[key].items = decorate(sidebar[key].items);
    }
  }
  return config;
}
