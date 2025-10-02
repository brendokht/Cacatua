"use client";

import { useForm } from "react-hook-form";
import { z } from "zod";
import { Button } from "../../components/ui/Button";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "../../components/ui/Form";
import { Input } from "../../components/ui/Input";
import { zodResolver } from "@hookform/resolvers/zod";
import { Checkbox } from "../../components/ui/Checkbox";
import { Separator } from "../../components/ui/Separator";
import { useNavigate } from "react-router-dom";
import { SunIcon } from "@radix-ui/react-icons";
import { useAuth } from "../../contexts/AuthContext/AuthContext";
import { loginFormSchema } from "../../schemas/LoginSchema";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "../../components/ui/AlertDialog";
import { useState } from "react";

export default function Login() {
  const [openDialog, setOpenDialog] = useState<boolean>(false);
  const [dialogMessage, setDialogMessage] = useState<string>("");

  const navigate = useNavigate();

  const auth = useAuth();

  const form = useForm<z.infer<typeof loginFormSchema>>({
    resolver: zodResolver(loginFormSchema),
    defaultValues: {
      email: "",
      password: "",
      rememberMe: false,
    },
  });

  async function onSubmit(values: z.infer<typeof loginFormSchema>) {
    let response;
    try {
      response = await auth.loginAction(values);
      if (response.success) {
        navigate("/");
      }
      // TODO: Alert that the log in failed
      else {
        setDialogMessage("Invalid credentials. Please try again.");
        // setDialogMessage(
        //   response?.message ?? "Error logging in. Please try again."
        // );
        setOpenDialog(true);
      }
    } catch (err) {
      setDialogMessage(
        response?.message ?? "Error logging in. Please try again."
      );
      setOpenDialog(true);
    }
  }

  const toggleTheme = () => {
    if (auth.theme === "dark") {
      auth.setTheme("light");
      localStorage.setItem("theme", "light");
      document.body.classList.add("light");
      document.body.classList.remove("dark");
    } else {
      auth.setTheme("dark");
      localStorage.setItem("theme", "dark");
      document.body.classList.add("dark");
      document.body.classList.remove("light");
    }
  };

  const clickForgotPassword = () => {
    console.log("pretending to go to forgot password");
  };

  const clickSignUp = () => navigate("/signup");

  const handleDialogClose = (isOpen: boolean) => {
    setOpenDialog(isOpen);
  };

  const mainClasses = `flex justify-center items-center bg-background min-h-full ${auth.theme}`;

  return (
    <>
      <AlertDialog open={openDialog} onOpenChange={handleDialogClose}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Failed</AlertDialogTitle>
            <AlertDialogDescription>{dialogMessage}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogAction onClick={() => setOpenDialog(false)}>
              Ok
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <div className={mainClasses}>
        <Form {...form}>
          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="bg-neutral-100 shadow-lg dark:shadow-zinc-700 dark:bg-neutral-900 space-y-2 w-96 flex flex-col justify-center p-4 rounded-md"
          >
            <h1 className="text-2xl font-semibold">Log in</h1>
            <FormField
              control={form.control}
              name="email"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Email</FormLabel>
                  <FormControl>
                    <Input
                      placeholder="xxx@xxx.com"
                      id="email"
                      type="email"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="password"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Password</FormLabel>
                  <FormControl>
                    <Input
                      placeholder="xxxxxx..."
                      type="password"
                      id="password"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="rememberMe"
              render={({ field }) => (
                <FormItem className="space-x-3 space-y-0 flex flex-row items-center">
                  <FormControl>
                    <Checkbox
                      checked={field.value}
                      onCheckedChange={field.onChange}
                    />
                  </FormControl>
                  <FormLabel className="space-y-1 leading-none">
                    Remember Me?
                  </FormLabel>
                </FormItem>
              )}
            />
            <Button
              variant="link"
              type="button"
              onClick={clickForgotPassword}
              className="text-xs text-gray-400 p-0 h-fit"
            >
              Forgot password
            </Button>
            <Button className="w-full" type="submit">
              Log in
            </Button>
            <Button
              variant="link"
              type="button"
              onClick={clickSignUp}
              className="text-xs text-gray-400 p-0 h-fit"
            >
              Not a member? Create a new account
            </Button>
            <Separator />
            <div className="flex justify-end">
              <Button
                type="button"
                variant="outline"
                size="icon"
                onClick={toggleTheme}
              >
                <SunIcon />
              </Button>
            </div>
          </form>
        </Form>
      </div>
    </>
  );
}
