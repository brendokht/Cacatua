import React, { useEffect, useState } from "react";
import { useAuth } from "../../contexts/AuthContext/AuthContext";
import { doc, DocumentReference, getDoc } from "firebase/firestore";
import { db } from "../../firebase/auth";
import {
  Avatar,
  AvatarFallback,
  AvatarImage,
} from "../../components/ui/Avatar";
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
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
} from "../../components/ui/Form";
import { rolesSchema } from "../../schemas/RolesSchema";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Input } from "../../components/ui/Input";
import { RequestType } from "../../enums/RequestType";
import { useOutletContext } from "react-router-dom";

interface UserData {
  uid: string;
  displayName: string;
  photoUrl: string;
}

interface FlockMemberData {
  display: string;
  id: string;
  photoUrl: string;
  roles: Array<string>;
}

interface UserRoleData {
  id: string;
  users?: Array<DocumentReference>;
  role: string;
}

interface RoleData {
  id: string;
  role: string;
}

type OutletContext = {
  handleUpdate: () => void;
};

export default function Roles() {
  const auth = useAuth();

  const { handleUpdate } = useOutletContext<OutletContext>();

  const [flockMembers, setFlockMembers] = useState<Array<FlockMemberData>>([]);
  const [roles, setRoles] = useState<Array<RoleData>>([]);
  const [selectedRole, setSelectedRole] = useState<RoleData>();

  const fetchData = async () => {
    if (!auth.selectedServer) {
      console.error("Selected server is undefined");
      return;
    }
    const docRef = doc(db, "flocks", auth.selectedServer);
    const docSnap = await getDoc(docRef);

    const docData = docSnap.data();

    const membersRef: Array<DocumentReference> = docData["users"];
    const members: Array<FlockMemberData> = [];
    const rolesData: Array<UserRoleData> = docData["roles"] ?? [];

    const rolesArray: Array<RoleData> = [{ id: "member", role: "Member" }];
    rolesData.forEach((role) => {
      rolesArray.push(role);
    });
    setRoles(rolesArray);

    if (membersRef && rolesData) {
      const memberPromises = membersRef.map(async (memberRef) => {
        const memberDoc = await getDoc(memberRef);

        const member = {
          displayName: memberDoc.get("displayName"),
          uid: memberDoc.get("uid"),
          photoUrl: memberDoc.get("photoUrl"),
        } as UserData;

        const memberRoles = rolesData
          .filter((r) => r.users?.some((e) => e.id === memberRef.id))
          .map((r) => r.role);

        const data: FlockMemberData = {
          display: member.displayName,
          id: member.uid,
          photoUrl: member.photoUrl,
          roles: memberRoles.length > 0 ? memberRoles : ["Member"],
        };

        members.push(data);
      });
      await Promise.all(memberPromises);

      setFlockMembers(members);
    } else {
      setFlockMembers([]);
    }
  };

  async function updateRole(values: z.infer<typeof rolesSchema>) {
    values.roleId = selectedRole.id;
    try {
      const response = await auth.fetchRequest(
        "Flock/update-role-async",
        values,
        RequestType.POST
      );
    } catch (err) {
      console.error("ERROR: ", err);
    } finally {
      await fetchData();
      form.reset();
      handleUpdate();
      setSelectedRole(null);
    }
  }

  async function deleteRole(values: z.infer<typeof rolesSchema>) {
    values.roleId = selectedRole.id;
    try {
      const response = await auth.fetchRequest(
        "Flock/delete-role-async",
        values,
        RequestType.DELETE
      );
    } catch (err) {
      console.error("ERROR: ", err);
    } finally {
      await fetchData();
      form.reset();
      handleUpdate();
      setSelectedRole(null);
    }
  }

  async function addRole(values: z.infer<typeof rolesSchema>) {
    try {
      const response = await auth.fetchRequest(
        "Flock/add-role-async",
        values,
        RequestType.POST
      );
    } catch (err) {
      console.error("ERROR: ", err);
    } finally {
      await fetchData();
      form.reset();
      handleUpdate();
      setSelectedRole(null);
    }
  }

  useEffect(() => {
    fetchData();
  }, []);

  const form = useForm<z.infer<typeof rolesSchema>>({
    resolver: zodResolver(rolesSchema),
    defaultValues: {
      flockId: auth.selectedServer,
      roleId: "",
      roleName: "",
      userIds: flockMembers
        .filter(
          (member) => selectedRole && member.roles.includes(selectedRole.role)
        )
        .map((member) => member.id),
    },
  });

  useEffect(() => {
    form.reset({
      flockId: auth.selectedServer,
      roleId: "",
      roleName:
        roles.find((r) => selectedRole && r.id === selectedRole.id)?.role || "",
      userIds: flockMembers
        .filter(
          (member) => selectedRole && member.roles.includes(selectedRole.role)
        )
        .map((member) => member.id),
    });
  }, [selectedRole, flockMembers, form, auth.selectedServer, roles]);

  return (
    <div className="h-[90%] flex flex-col">
      <h1 className="text-3xl font-bold">Roles</h1>
      <div className="space-y-2">
        {roles.map((role) => (
          <div className="mt-4 space-y-2" key={role.id}>
            <div className="flex flex-row items-center space-x-2">
              <h2 className="text-xl font-semibold">{role.role}</h2>
              {role.id !== "member" ? (
                <Dialog>
                  <DialogTrigger asChild>
                    <a
                      className="text-xs text-primary hover:cursor-pointer"
                      onClick={() => setSelectedRole(role)}
                    >
                      Edit
                    </a>
                  </DialogTrigger>
                  <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                      <DialogTitle>Update Role "{role.role}"</DialogTitle>
                      <DialogDescription>
                        Update the "{role.role}" role for your flock
                      </DialogDescription>
                    </DialogHeader>
                    <div className="flex items-center w-full">
                      <Form {...form}>
                        <form
                          onSubmit={form.handleSubmit(updateRole)}
                          className="space-y-4 w-full"
                        >
                          <FormField
                            control={form.control}
                            name="roleName"
                            render={({ field }) => (
                              <FormItem>
                                <FormLabel>Role Name</FormLabel>
                                <FormControl>
                                  <Input id="roleName" {...field} />
                                </FormControl>
                              </FormItem>
                            )}
                          />
                          <FormField
                            control={form.control}
                            name="userIds"
                            render={({ field }) => (
                              <FormItem>
                                <FormLabel>Assign Users</FormLabel>
                                <FormControl>
                                  <div className="space-y-2">
                                    {flockMembers.map((member) => (
                                      <div
                                        key={member.id}
                                        className="flex items-center space-x-2"
                                      >
                                        <input
                                          type="checkbox"
                                          id={member.id}
                                          value={member.id}
                                          {...field}
                                          onChange={(e) => {
                                            if (e.target.checked) {
                                              field.onChange([
                                                ...field.value,
                                                member.id,
                                              ]);
                                            } else {
                                              field.onChange(
                                                field.value.filter(
                                                  (id) => id !== member.id
                                                )
                                              );
                                            }
                                          }}
                                          checked={field.value.includes(
                                            member.id
                                          )}
                                        />
                                        <label htmlFor={member.id}>
                                          {member.display}
                                        </label>
                                      </div>
                                    ))}
                                  </div>
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
                                variant="destructive"
                                onClick={() => form.handleSubmit(deleteRole)()}
                              >
                                Delete
                              </Button>
                            </DialogClose>
                            <DialogClose asChild>
                              <Button type="button" variant="secondary">
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
            {flockMembers.filter((member) => member.roles.includes(role.role))
              .length === 0 ? (
              <div className="text-sm text-primary-foreground">
                No users in this role
              </div>
            ) : (
              flockMembers
                .filter((member) => member.roles.includes(role.role))
                .map((member) => (
                  <div
                    data-key={member.id}
                    key={member.id}
                    className="text-primary-foreground flex flex-row items-center space-x-2"
                  >
                    <Avatar>
                      <AvatarImage src={member.photoUrl} />
                      <AvatarFallback>
                        {member.display.charAt(0)}
                      </AvatarFallback>
                    </Avatar>
                    <div className="flex flex-col">
                      <div>{member.display}</div>
                    </div>
                  </div>
                ))
            )}
          </div>
        ))}
      </div>
      <div className="flex-1" />
      <div className="flex flex-row space-x-2">
        <Dialog>
          <DialogTrigger asChild>
            <Button variant="default" className="w-fit">
              Add Role
            </Button>
          </DialogTrigger>
          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle>Add Role</DialogTitle>
              <DialogDescription>
                Add a new role to your flock
              </DialogDescription>
            </DialogHeader>
            <div className="flex items-center w-full">
              <Form {...form}>
                <form
                  onSubmit={form.handleSubmit(addRole)}
                  className="space-y-4 w-full"
                >
                  <FormField
                    control={form.control}
                    name="roleName"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Role Name</FormLabel>
                        <FormControl>
                          <Input id="roleName" {...field} />
                        </FormControl>
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="userIds"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Assign Users</FormLabel>
                        <FormControl>
                          <div className="space-y-2">
                            {flockMembers.map((member) => (
                              <div
                                key={member.id}
                                className="flex items-center space-x-2"
                              >
                                <input
                                  type="checkbox"
                                  id={member.id}
                                  value={member.id}
                                  {...field}
                                  onChange={(e) => {
                                    if (e.target.checked) {
                                      field.onChange([
                                        ...field.value,
                                        member.id,
                                      ]);
                                    } else {
                                      field.onChange(
                                        field.value.filter(
                                          (id) => id !== member.id
                                        )
                                      );
                                    }
                                  }}
                                  checked={field.value.includes(member.id)}
                                />
                                <label htmlFor={member.id}>
                                  {member.display}
                                </label>
                              </div>
                            ))}
                          </div>
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
                      <Button type="button" variant="secondary">
                        Close
                      </Button>
                    </DialogClose>
                  </DialogFooter>
                </form>
              </Form>
            </div>
          </DialogContent>
        </Dialog>
      </div>
    </div>
  );
}
