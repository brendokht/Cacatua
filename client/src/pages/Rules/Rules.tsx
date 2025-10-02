import React, { useEffect, useState } from "react";
import { useAuth } from "../../contexts/AuthContext/AuthContext";
import { RequestType } from "../../enums/RequestType";
import { Button } from "../../components/ui/Button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "../../components/ui/Dialog";
import { PlusIcon } from "@radix-ui/react-icons";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
} from "../../components/ui/Form";
import { updateRulesSchema } from "../../schemas/UpdateRulesScheme";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Textarea } from "../../components/ui/TextArea";
export default function Rules() {
  const auth = useAuth();
  const [rules, setRules] = useState<string>(null);

  const form = useForm<z.infer<typeof updateRulesSchema>>({
    resolver: zodResolver(updateRulesSchema),
    defaultValues: {
      flockId: auth.selectedServer,
      newRules: "",
    },
  });

  const fetchData = async () => {
    const response = await auth.fetchRequest(
      `Flock/get-rules/${auth.selectedServer}`,
      null,
      RequestType.GET
    );
    if (response.status !== 200) {
      setRules("No rules yet!");
      form.setValue("newRules", "No rules yet!");
    } else {
      const data = await response.text();
      setRules(data);
      form.setValue("newRules", data);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  async function updateRules(values: z.infer<typeof updateRulesSchema>) {
    try {
      const response = await auth.fetchRequest(
        "Flock/update-rules-async",
        values,
        RequestType.POST
      );
    } catch (err) {
      console.error("ERROR: ", err);
    } finally {
      await fetchData();
    }
  }

  return (
    <div className="h-[90%] flex flex-col">
      <h1 className="text-3xl font-bold">Rules</h1>
      <div className="whitespace-pre-wrap">{rules}</div>
      <div className="flex-1"></div>
      {auth.isOwner ? (
        <Dialog>
          <DialogTrigger asChild>
            <Button variant="default" className="w-fit">
              Update Rules
            </Button>
          </DialogTrigger>
          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle>Update Flock Rules</DialogTitle>
              <DialogDescription>
                Create or update the rules for your flock!
              </DialogDescription>
            </DialogHeader>
            <div className="flex items-center w-full">
              <Form {...form}>
                <form
                  onSubmit={form.handleSubmit(updateRules)}
                  className="space-y-4 w-full"
                >
                  <FormField
                    control={form.control}
                    name="newRules"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>New Rules</FormLabel>
                        <FormControl>
                          <Textarea
                            className="w-full h-40"
                            id="newRules"
                            {...field}
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />
                  <DialogFooter className="sm:justify-start">
                    <DialogClose asChild>
                      <Button type="submit" variant="default">
                        Submit
                      </Button>
                    </DialogClose>
                    <DialogClose asChild>
                      <Button
                        type="button"
                        variant="secondary"
                        onClick={() => form.setValue("newRules", rules)}
                      >
                        Close
                      </Button>
                    </DialogClose>
                  </DialogFooter>
                </form>
              </Form>
            </div>
          </DialogContent>
        </Dialog>
      ) : null}
    </div>
  );
}
