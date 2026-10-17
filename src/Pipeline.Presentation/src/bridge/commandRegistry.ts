// Rule: L-02 One concept, one home (Amendment A-1)
import { COMMANDS, CommandName, CommandDescriptor } from '../contracts/commands';

export const COMMAND_REGISTRY = new Map<CommandName, CommandDescriptor>(
  COMMANDS.map(c => [c.command, c])
);

export function isRegisteredCommand(command: string): command is CommandName {
  return COMMAND_REGISTRY.has(command as CommandName);
}

export function getCommandDescriptor(command: CommandName): CommandDescriptor {
  const descriptor = COMMAND_REGISTRY.get(command);
  if (!descriptor) {
    throw new Error(`Unknown command '${command}' is not registered in COMMANDS.`);
  }
  return descriptor;
}
