import { z } from "zod";

export const createFlockSchema = z.object({
  userId: z.string(),
  flockName: z.string().min(3).max(35),
});
