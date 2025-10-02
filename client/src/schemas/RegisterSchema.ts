import { z } from "zod";

export const signupFormSchema = z
  .object({
    username: z
      .string({ required_error: "Username is required" })
      .min(3, "Username must be at least 3 characters")
      .max(15, "Username must be maximum 15 characters")
      .trim(),
    email: z
      .string({ required_error: "Email is required" })
      .email({ message: "Must be in format xxx@xxx.xxx" })
      .trim(),
    firstname: z
      .string({ required_error: "First name is required" })
      .min(2, "First name must be at least 2 characters")
      .max(15, "First name must be maximum 15 characters"),
    lastname: z
      .string({ required_error: "Last name is required" })
      .min(2, "Last name must be at least 2 characters")
      .max(15, "Last name must be maximum 15 characters"),
    password: z
      .string({ required_error: "Password is required" })
      .min(6, "Password must be at least 6 characters"),
    confirmPassword: z.string({
      required_error: "Confirm password is required",
    }),
  })
  .required()
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords don't match",
    path: ["confirmPassword"],
  });
