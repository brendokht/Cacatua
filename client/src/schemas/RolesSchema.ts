import { z } from "zod";

export const rolesSchema = z.object({
  flockId: z.string(),
  roleId: z.string(),
  roleName: z.string(),
  userIds: z.array(z.string()),
});
