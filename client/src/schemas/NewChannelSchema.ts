import { z } from "zod";

export const newChannelSchema = z.object({
  name: z.string().min(3).max(35),
});
