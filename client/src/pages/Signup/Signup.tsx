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
import { Separator } from "../../components/ui/Separator";
import { useNavigate } from "react-router-dom";
import { SunIcon } from "@radix-ui/react-icons";
import { signupFormSchema } from "../../schemas/RegisterSchema";
import { useAuth } from "../../contexts/AuthContext/AuthContext";
import {
  AlertDialog,
  AlertDialogDescription,
  AlertDialogAction,
  AlertDialogContent,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "../../components/ui/AlertDialog";
import { useState } from "react";

export default function Signup() {
  const [openDialog, setOpenDialog] = useState<boolean>(false);
  const [shouldNavigate, setShouldNavigate] = useState<boolean>(false);
  const [dialogMessage, setDialogMessage] = useState<string>("");
  const auth = useAuth();
  const navigate = useNavigate();

  const form = useForm<z.infer<typeof signupFormSchema>>({
    resolver: zodResolver(signupFormSchema),
    defaultValues: {
      username: "",
      email: "",
      firstname: "",
      lastname: "",
      password: "",
      confirmPassword: "",
    },
  });

  async function onSubmit(values: z.infer<typeof signupFormSchema>) {
    const response = await auth.registerAction(values);

    setDialogMessage(response.message);
    setOpenDialog(true);

    if (response.success) setShouldNavigate(true);
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

  const clickLogin = () => {
    navigate("/login");
  };

  const handleDialogClose = (isOpen: boolean) => {
    setOpenDialog(isOpen);

    if (!isOpen && shouldNavigate) {
      setTimeout(() => {
        navigate("/login");
      }, 500);
    }
  };

  const mainClasses = `flex justify-center items-center bg-background min-h-full ${auth.theme}`;

  return (
    <>
      <AlertDialog open={openDialog} onOpenChange={handleDialogClose}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {shouldNavigate ? "Success" : "Failed"}
            </AlertDialogTitle>
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
            <h1 className="text-2xl font-semibold">Sign up</h1>
            <FormField
              control={form.control}
              name="username"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Username</FormLabel>
                  <FormControl>
                    <Input
                      placeholder="xxx..."
                      id="username"
                      type="text"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="email"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Email</FormLabel>
                  <FormControl>
                    <Input
                      placeholder="xxx@xxx.com"
                      type="email"
                      id="email"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="firstname"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>First Name</FormLabel>
                  <FormControl>
                    <Input
                      placeholder="First name"
                      type="text"
                      id="firstname"
                      autoComplete="family-name"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="lastname"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Last Name</FormLabel>
                  <FormControl>
                    <Input
                      placeholder="Last name"
                      type="text"
                      id="lastname"
                      autoComplete="family-name"
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
              name="confirmPassword"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Confirm password</FormLabel>
                  <FormControl>
                    <Input
                      placeholder="xxxxxx..."
                      type="password"
                      id="confirmPassword"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <Button
              variant="link"
              type="button"
              onClick={clickLogin}
              className="text-xs text-gray-400 p-0 h-fit"
            >
              Already a member? Log in
            </Button>
            <Button className="w-full" type="submit">
              Sign up
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
