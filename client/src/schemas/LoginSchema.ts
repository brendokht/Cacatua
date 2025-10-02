import { z } from "zod";

export const loginFormSchema = z.object({
  email: z
    .string({ required_error: "Email is required" })
    .email({ message: "Must be in format xxx@xxx.xxx" })
    .trim(),
  password: z.string({ required_error: "Password is required" }),
  rememberMe: z.boolean().optional(),
});
