/**
 * 场景接口定义 —— 所有场景脚本必须实现此接口
 */
import type { ScenarioContext, ScenarioDefinition, ScenarioResult } from '../model/scenario-model.js';

/** 场景执行器接口 */
export interface IScenario {
  /** 场景静态定义 */
  readonly definition: ScenarioDefinition;
  /** 执行场景采集逻辑 */
  execute(ctx: ScenarioContext): Promise<ScenarioResult>;
}
