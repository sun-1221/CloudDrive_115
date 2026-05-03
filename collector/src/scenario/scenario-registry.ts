/**
 * 场景注册表 —— 管理所有可用场景
 */
import type { IScenario } from './scenario.types.js';

export class ScenarioRegistry {
  private readonly scenarios = new Map<string, IScenario>();

  register(scenario: IScenario): void {
    this.scenarios.set(scenario.definition.id, scenario);
  }

  get(id: string): IScenario | undefined {
    return this.scenarios.get(id);
  }

  getAll(): IScenario[] {
    return [...this.scenarios.values()];
  }

  getByIds(ids: string[]): IScenario[] {
    if (ids.length === 0) return this.getAll();
    return ids.map((id) => {
      const s = this.scenarios.get(id);
      if (!s) throw new Error(`未找到场景: ${id}`);
      return s;
    });
  }

  getByTag(tag: string): IScenario[] {
    return this.getAll().filter((s) => s.definition.tags.includes(tag));
  }
}
