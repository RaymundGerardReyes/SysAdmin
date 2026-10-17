export interface Breadcrumb {
  readonly timestamp: string;
  readonly event: string;
  readonly payload?: Record<string, unknown>;
}

class BreadcrumbRingBuffer {
  private buffer: Breadcrumb[] = [];
  private readonly capacity = 200;

  add(breadcrumb: Breadcrumb): void {
    if (this.buffer.length >= this.capacity) {
      this.buffer.shift();
    }
    this.buffer.push(breadcrumb);
  }

  getAll(): readonly Breadcrumb[] {
    return [...this.buffer];
  }

  clear(): void {
    this.buffer = [];
  }
}

export const breadcrumbs = new BreadcrumbRingBuffer();
