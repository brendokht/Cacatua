import { z } from "zod";

export const updateRulesSchema = z.object({
  flockId: z.string(),
  newRules: z.string(),
});
