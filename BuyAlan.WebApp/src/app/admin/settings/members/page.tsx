"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { z } from "zod";
import { Alert } from "@/components/admin/alert";
import { Badge } from "@/components/admin/badge";
import { Button } from "@/components/admin/button";
import { Card } from "@/components/admin/card";
import {
  Drawer,
  DrawerBody,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
} from "@/components/admin/drawer";
import { Input } from "@/components/admin/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/admin/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRoot,
  TableRow,
} from "@/components/admin/table";
import {
  getSubscriptionsBySubscriptionIdMembersInvitationsByInvitationIdLink,
  type GetSubscriptionMembersResult,
  type PostSubscriptionInvitationInput,
  type SubscriptionInvitationItem,
  type SubscriptionMemberItem,
  type SubscriptionUserRole,
} from "@/lib/api";
import {
  deleteSubscriptionsBySubscriptionIdMembersByMemberUserIdMutation,
  deleteSubscriptionsBySubscriptionIdMembersInvitationsByInvitationIdMutation,
  getSubscriptionsBySubscriptionIdMembersOptions,
  getSubscriptionsBySubscriptionIdMembersQueryKey,
  postSubscriptionsBySubscriptionIdMembersInvitationsByInvitationIdResendMutation,
  postSubscriptionsBySubscriptionIdMembersInvitationsMutation,
  putSubscriptionsBySubscriptionIdMembersByMemberUserIdRoleMutation,
} from "@/lib/api/@tanstack/react-query.gen";
import { useFeatureFlag } from "@/lib/feature-flags";
import { useSession } from "@/lib/session-context";

type FeedbackState = {
  tone: "error" | "success";
  title: string;
  text: string;
} | null;

type ApiErrorLike = {
  message?: string;
  detail?: string;
};

type InvitationFormValues = {
  email: string;
  role: SubscriptionUserRole;
};

type MemberRoleFormValues = {
  role: SubscriptionUserRole;
};

const DEFAULT_MEMBERS_ERROR = "Unable to load members for this subscription.";
const DEFAULT_CREATE_ERROR = "Unable to send that invitation.";
const DEFAULT_RESEND_ERROR = "Unable to resend that invitation.";
const DEFAULT_COPY_ERROR = "Unable to copy that invitation link.";
const DEFAULT_DELETE_INVITATION_ERROR = "Unable to revoke that invitation.";
const DEFAULT_UPDATE_ROLE_ERROR = "Unable to update that team member role.";
const DEFAULT_DELETE_MEMBER_ERROR = "Unable to remove that team member.";
const MISSING_SUBSCRIPTION_ERROR = "No active subscription is available for this account.";
const OWNER_ROLE = 0 as SubscriptionUserRole;
const MEMBER_ROLE = 1 as SubscriptionUserRole;

const roleLabelByValue: Record<number, string> = {
  [OWNER_ROLE]: "Owner",
  [MEMBER_ROLE]: "Member",
};

const invitationSchema = z.object({
  email: z.string().trim().min(1, "Email is required.").email("Use a valid email format."),
  role: z.union([z.literal(OWNER_ROLE), z.literal(MEMBER_ROLE)]),
});

const memberRoleSchema = z.object({
  role: z.union([z.literal(OWNER_ROLE), z.literal(MEMBER_ROLE)]),
});

const resolveApiErrorMessage = (error: unknown, fallback: string): string => {
  if (!error || typeof error !== "object") {
    return fallback;
  }

  const apiError = error as ApiErrorLike;

  if (typeof apiError.message === "string" && apiError.message.trim().length > 0) {
    return apiError.message;
  }

  if (typeof apiError.detail === "string" && apiError.detail.trim().length > 0) {
    return apiError.detail;
  }

  return fallback;
};

const getRoleLabel = (role: SubscriptionUserRole): string => {
  return roleLabelByValue[role] ?? "Member";
};

const formatDateTime = (value: string | null): string => {
  if (!value) {
    return "Not available";
  }

  const parsedDate = new Date(value);
  if (Number.isNaN(parsedDate.getTime())) {
    return "Not available";
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(parsedDate);
};

const formatInvitationStatus = (status: string): string => {
  if (status.length === 0) {
    return "Unknown";
  }

  return status.replaceAll("_", " ");
};

const getInvitationStatusVariant = (
  status: string,
): "default" | "error" | "neutral" | "success" | "warning" => {
  switch (status) {
    case "accepted":
      return "success";
    case "revoked":
      return "error";
    case "expired":
      return "warning";
    case "pending":
      return "warning";
    default:
      return "neutral";
  }
};

const MembersSummaryCards = ({
  membersResult,
}: {
  membersResult: GetSubscriptionMembersResult;
}) => {
  const pendingInvitations = membersResult.invitations.filter((item) => item.status === "pending");
  const ownerCount = membersResult.members.filter((item) => item.role === OWNER_ROLE).length;

  const items = [
    {
      label: "Pending invitations",
      value: pendingInvitations.length.toString(),
    },
    {
      label: "Current members",
      value: membersResult.members.length.toString(),
    },
    {
      label: "Owners",
      value: ownerCount.toString(),
    },
  ];

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
      {items.map((item) => (
        <div key={item.label} className="rounded-lg border border-gray-200 bg-gray-50 p-4">
          <p className="text-xs font-medium uppercase tracking-wide text-gray-500">{item.label}</p>
          <p className="mt-2 text-lg font-semibold text-gray-900">{item.value}</p>
        </div>
      ))}
    </div>
  );
};

export default function SettingsMembersPage() {
  const isTeamMembersEnabled = useFeatureFlag("teamMembers");
  const queryClient = useQueryClient();
  const { currentUser, errorMessage: sessionErrorMessage, isLoading: isSessionLoading } = useSession();
  const [feedback, setFeedback] = useState<FeedbackState>(null);
  const [isInviteDrawerOpen, setIsInviteDrawerOpen] = useState<boolean>(false);
  const [isDeleteInvitationDrawerOpen, setIsDeleteInvitationDrawerOpen] = useState<boolean>(false);
  const [isMemberRoleDrawerOpen, setIsMemberRoleDrawerOpen] = useState<boolean>(false);
  const [isDeleteMemberDrawerOpen, setIsDeleteMemberDrawerOpen] = useState<boolean>(false);
  const [selectedInvitation, setSelectedInvitation] = useState<SubscriptionInvitationItem | null>(null);
  const [selectedMember, setSelectedMember] = useState<SubscriptionMemberItem | null>(null);

  const subscriptionId = currentUser?.activeSubscriptionId ?? null;

  const membersQuery = useQuery({
    ...getSubscriptionsBySubscriptionIdMembersOptions({
      path: {
        subscriptionId: subscriptionId ?? "",
      },
    }),
    enabled: subscriptionId !== null && isTeamMembersEnabled,
    retry: false,
  });

  const createInvitationMutation = useMutation(postSubscriptionsBySubscriptionIdMembersInvitationsMutation());
  const resendInvitationMutation = useMutation(postSubscriptionsBySubscriptionIdMembersInvitationsByInvitationIdResendMutation());
  const deleteInvitationMutation = useMutation(deleteSubscriptionsBySubscriptionIdMembersInvitationsByInvitationIdMutation());
  const updateMemberRoleMutation = useMutation(putSubscriptionsBySubscriptionIdMembersByMemberUserIdRoleMutation());
  const deleteMemberMutation = useMutation(deleteSubscriptionsBySubscriptionIdMembersByMemberUserIdMutation());

  const invitationForm = useForm<InvitationFormValues>({
    resolver: zodResolver(invitationSchema),
    defaultValues: {
      email: "",
      role: MEMBER_ROLE,
    },
  });

  const memberRoleForm = useForm<MemberRoleFormValues>({
    resolver: zodResolver(memberRoleSchema),
    defaultValues: {
      role: MEMBER_ROLE,
    },
  });

  useEffect(() => {
    const availableRoles = membersQuery.data?.availableRoles ?? [OWNER_ROLE, MEMBER_ROLE];
    const firstRole = availableRoles[0] ?? MEMBER_ROLE;
    const currentRole = invitationForm.getValues("role");
    const nextRole = availableRoles.includes(currentRole) ? currentRole : firstRole;

    invitationForm.reset({
      email: invitationForm.getValues("email"),
      role: nextRole,
    });
  }, [invitationForm, membersQuery.data?.availableRoles]);

  useEffect(() => {
    if (!selectedMember) {
      return;
    }

    memberRoleForm.reset({
      role: selectedMember.role,
    });
  }, [memberRoleForm, selectedMember]);

  const refreshMembersAsync = async (): Promise<void> => {
    if (!subscriptionId) {
      return;
    }

    await queryClient.invalidateQueries({
      queryKey: getSubscriptionsBySubscriptionIdMembersQueryKey({
        path: {
          subscriptionId,
        },
      }),
    });

    await membersQuery.refetch();
  };

  const openInviteDrawer = (): void => {
    setFeedback(null);
    setIsInviteDrawerOpen(true);
  };

  const openDeleteInvitationDrawer = (invitation: SubscriptionInvitationItem): void => {
    setFeedback(null);
    setSelectedInvitation(invitation);
    setIsDeleteInvitationDrawerOpen(true);
  };

  const openMemberRoleDrawer = (member: SubscriptionMemberItem): void => {
    setFeedback(null);
    setSelectedMember(member);
    setIsMemberRoleDrawerOpen(true);
  };

  const openDeleteMemberDrawer = (member: SubscriptionMemberItem): void => {
    setFeedback(null);
    setSelectedMember(member);
    setIsDeleteMemberDrawerOpen(true);
  };

  const closeInviteDrawer = (): void => {
    setIsInviteDrawerOpen(false);
    invitationForm.reset({
      email: "",
      role: membersQuery.data?.availableRoles[0] ?? MEMBER_ROLE,
    });
  };

  const closeDeleteInvitationDrawer = (): void => {
    setIsDeleteInvitationDrawerOpen(false);
    setSelectedInvitation(null);
  };

  const closeMemberRoleDrawer = (): void => {
    setIsMemberRoleDrawerOpen(false);
    setSelectedMember(null);
  };

  const closeDeleteMemberDrawer = (): void => {
    setIsDeleteMemberDrawerOpen(false);
    setSelectedMember(null);
  };

  const handleInviteCreate = invitationForm.handleSubmit(async (values) => {
    if (!subscriptionId) {
      setFeedback({
        tone: "error",
        title: "Subscription required",
        text: MISSING_SUBSCRIPTION_ERROR,
      });
      return;
    }

    setFeedback(null);

    const requestBody: PostSubscriptionInvitationInput = {
      email: values.email.trim(),
      role: values.role,
    };

    try {
      const invitation = await createInvitationMutation.mutateAsync({
        path: {
          subscriptionId,
        },
        body: requestBody,
      });

      await refreshMembersAsync();
      closeInviteDrawer();
      setFeedback({
        tone: "success",
        title: "Invitation queued",
        text: `Invitation sent to ${invitation.email}.`,
      });
    } catch (error: unknown) {
      setFeedback({
        tone: "error",
        title: "Unable to send invitation",
        text: resolveApiErrorMessage(error, DEFAULT_CREATE_ERROR),
      });
    }
  });

  const handleInvitationResend = async (invitation: SubscriptionInvitationItem): Promise<void> => {
    if (!subscriptionId) {
      setFeedback({
        tone: "error",
        title: "Subscription required",
        text: MISSING_SUBSCRIPTION_ERROR,
      });
      return;
    }

    setFeedback(null);

    try {
      await resendInvitationMutation.mutateAsync({
        path: {
          subscriptionId,
          invitationId: invitation.invitationId,
        },
      });

      await refreshMembersAsync();
      setFeedback({
        tone: "success",
        title: "Invitation resent",
        text: `Invitation resent to ${invitation.email}.`,
      });
    } catch (error: unknown) {
      setFeedback({
        tone: "error",
        title: "Unable to resend invitation",
        text: resolveApiErrorMessage(error, DEFAULT_RESEND_ERROR),
      });
    }
  };

  const handleInvitationCopy = async (invitation: SubscriptionInvitationItem): Promise<void> => {
    if (!subscriptionId) {
      setFeedback({
        tone: "error",
        title: "Subscription required",
        text: MISSING_SUBSCRIPTION_ERROR,
      });
      return;
    }

    setFeedback(null);

    try {
      const response = await getSubscriptionsBySubscriptionIdMembersInvitationsByInvitationIdLink({
        path: {
          subscriptionId,
          invitationId: invitation.invitationId,
        },
        throwOnError: true,
      });

      const invitationUrl = response.data.invitationUrl;

      if (!navigator.clipboard || typeof navigator.clipboard.writeText !== "function") {
        throw new Error("Clipboard access is not available in this browser.");
      }

      await navigator.clipboard.writeText(invitationUrl);
      setFeedback({
        tone: "success",
        title: "Link copied",
        text: `Invitation link copied for ${invitation.email}.`,
      });
    } catch (error: unknown) {
      setFeedback({
        tone: "error",
        title: "Unable to copy invitation link",
        text: resolveApiErrorMessage(error, DEFAULT_COPY_ERROR),
      });
    }
  };

  const handleInvitationDelete = async (): Promise<void> => {
    if (!subscriptionId || !selectedInvitation) {
      setFeedback({
        tone: "error",
        title: "Invitation not available",
        text: DEFAULT_DELETE_INVITATION_ERROR,
      });
      return;
    }

    setFeedback(null);

    try {
      await deleteInvitationMutation.mutateAsync({
        path: {
          subscriptionId,
          invitationId: selectedInvitation.invitationId,
        },
      });

      await refreshMembersAsync();
      closeDeleteInvitationDrawer();
      setFeedback({
        tone: "success",
        title: "Invitation revoked",
        text: `Invitation revoked for ${selectedInvitation.email}.`,
      });
    } catch (error: unknown) {
      setFeedback({
        tone: "error",
        title: "Unable to revoke invitation",
        text: resolveApiErrorMessage(error, DEFAULT_DELETE_INVITATION_ERROR),
      });
    }
  };

  const handleMemberRoleSave = memberRoleForm.handleSubmit(async (values) => {
    if (!subscriptionId || !selectedMember) {
      setFeedback({
        tone: "error",
        title: "Member not available",
        text: DEFAULT_UPDATE_ROLE_ERROR,
      });
      return;
    }

    setFeedback(null);

    try {
      await updateMemberRoleMutation.mutateAsync({
        path: {
          subscriptionId,
          memberUserId: selectedMember.userId,
        },
        body: {
          role: values.role,
        },
      });

      await refreshMembersAsync();
      closeMemberRoleDrawer();
      setFeedback({
        tone: "success",
        title: "Role updated",
        text: `Updated ${selectedMember.email} to ${getRoleLabel(values.role)}.`,
      });
    } catch (error: unknown) {
      setFeedback({
        tone: "error",
        title: "Unable to update role",
        text: resolveApiErrorMessage(error, DEFAULT_UPDATE_ROLE_ERROR),
      });
    }
  });

  const handleMemberDelete = async (): Promise<void> => {
    if (!subscriptionId || !selectedMember) {
      setFeedback({
        tone: "error",
        title: "Member not available",
        text: DEFAULT_DELETE_MEMBER_ERROR,
      });
      return;
    }

    setFeedback(null);

    try {
      await deleteMemberMutation.mutateAsync({
        path: {
          subscriptionId,
          memberUserId: selectedMember.userId,
        },
      });

      await refreshMembersAsync();
      closeDeleteMemberDrawer();
      setFeedback({
        tone: "success",
        title: "Member removed",
        text: `${selectedMember.email} no longer has access to this subscription.`,
      });
    } catch (error: unknown) {
      setFeedback({
        tone: "error",
        title: "Unable to remove member",
        text: resolveApiErrorMessage(error, DEFAULT_DELETE_MEMBER_ERROR),
      });
    }
  };

  const availableRoles = membersQuery.data?.availableRoles ?? [OWNER_ROLE, MEMBER_ROLE];
  const pendingInvitations = membersQuery.data?.invitations.filter((item) => item.status === "pending") ?? [];
  const currentMembers = membersQuery.data?.members ?? [];
  const inviteEmailError = invitationForm.formState.errors.email?.message;
  const inviteRoleError = invitationForm.formState.errors.role?.message;
  const memberRoleError = memberRoleForm.formState.errors.role?.message;
  const isAnyMutationPending =
    createInvitationMutation.isPending ||
    resendInvitationMutation.isPending ||
    deleteInvitationMutation.isPending ||
    updateMemberRoleMutation.isPending ||
    deleteMemberMutation.isPending;

  if (!isTeamMembersEnabled) {
    return (
      <section className="m-4">
        <Alert title="Members unavailable" type="info">
          Team member management is disabled for this workspace right now. Invitations and member changes are unavailable until the feature flag is turned back on.
        </Alert>
      </section>
    );
  }

  if (isSessionLoading) {
    return <div className="p-4 text-sm text-gray-500">Loading member settings...</div>;
  }

  if (sessionErrorMessage) {
    return (
      <section className="m-4">
        <Alert title="Unable to load session" type="error">
          {sessionErrorMessage}
        </Alert>
      </section>
    );
  }

  if (!subscriptionId) {
    return (
      <section className="m-4">
        <Alert title="Subscription required" type="warn">
          {MISSING_SUBSCRIPTION_ERROR}
        </Alert>
      </section>
    );
  }

  return (
    <>
      <section className="m-4">
        <div className="grid grid-cols-1 gap-x-14 gap-y-8 md:grid-cols-3">
          <div>
            <h2 className="scroll-mt-10 text-sm font-semibold text-gray-900 dark:text-gray-50">
              Members and invitations
            </h2>
            <p className="mt-1 text-xs leading-6 text-gray-500">
              Manage who already has access to this subscription and who still needs to accept an invitation.
            </p>
          </div>
          <div className="space-y-4 md:col-span-2">
            {feedback ? (
              <Alert title={feedback.title} type={feedback.tone === "success" ? "info" : "error"}>
                {feedback.text}
              </Alert>
            ) : null}

            {membersQuery.error ? (
              <Alert title="Unable to load team access" type="error">
                {resolveApiErrorMessage(membersQuery.error, DEFAULT_MEMBERS_ERROR)}
              </Alert>
            ) : null}

            {membersQuery.isLoading && !membersQuery.data ? (
              <p className="text-sm text-gray-500">Loading team access...</p>
            ) : null}

            {membersQuery.data ? (
              <>
                <MembersSummaryCards membersResult={membersQuery.data} />

                <Card className="space-y-4">
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div>
                      <h3 className="text-sm font-semibold text-gray-900">Invite team member</h3>
                      <p className="mt-1 text-xs text-gray-500">
                        Create an owner or member invitation and send it through the shared email pipeline.
                      </p>
                    </div>
                    <div className="flex gap-3">
                      <Button
                        disabled={isAnyMutationPending}
                        onClick={openInviteDrawer}
                        type="button"
                        variant="primary"
                      >
                        Invite team member
                      </Button>
                      <Button
                        disabled={membersQuery.isFetching || isAnyMutationPending}
                        isLoading={membersQuery.isFetching}
                        onClick={() => void refreshMembersAsync()}
                        type="button"
                        variant="secondary"
                      >
                        Refresh
                      </Button>
                    </div>
                  </div>
                </Card>

                <Card className="space-y-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <h3 className="text-sm font-semibold text-gray-900">Pending invitations</h3>
                      <p className="mt-1 text-xs text-gray-500">
                        Resend, copy, or revoke invitations that have not been accepted yet.
                      </p>
                    </div>
                    <Badge variant="warning">{pendingInvitations.length} pending</Badge>
                  </div>

                  {pendingInvitations.length === 0 ? (
                    <Alert title="No pending invitations" type="info">
                      No invitations are waiting for acceptance right now.
                    </Alert>
                  ) : (
                    <TableRoot>
                      <Table>
                        <TableHead>
                          <TableRow>
                            <TableHeaderCell>Email</TableHeaderCell>
                            <TableHeaderCell>Role</TableHeaderCell>
                            <TableHeaderCell>Status</TableHeaderCell>
                            <TableHeaderCell>Sent</TableHeaderCell>
                            <TableHeaderCell>Invited by</TableHeaderCell>
                            <TableHeaderCell>Actions</TableHeaderCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {pendingInvitations.map((invitation) => (
                            <TableRow key={invitation.invitationId}>
                              <TableCell className="font-medium text-gray-900">{invitation.email}</TableCell>
                              <TableCell>{getRoleLabel(invitation.role)}</TableCell>
                              <TableCell>
                                <Badge variant={getInvitationStatusVariant(invitation.status)}>
                                  {formatInvitationStatus(invitation.status)}
                                </Badge>
                              </TableCell>
                              <TableCell>{formatDateTime(invitation.sentAtUtc)}</TableCell>
                              <TableCell>{invitation.invitedByDisplayName || invitation.invitedByUserId}</TableCell>
                              <TableCell>
                                <div className="flex flex-wrap gap-2">
                                  <Button
                                    disabled={!invitation.canResend || isAnyMutationPending}
                                    isLoading={
                                      resendInvitationMutation.isPending &&
                                      resendInvitationMutation.variables?.path.invitationId === invitation.invitationId
                                    }
                                    onClick={() => void handleInvitationResend(invitation)}
                                    type="button"
                                    variant="secondary"
                                  >
                                    Resend
                                  </Button>
                                  <Button
                                    disabled={!invitation.canCopyLink || isAnyMutationPending}
                                    onClick={() => void handleInvitationCopy(invitation)}
                                    type="button"
                                    variant="secondary"
                                  >
                                    Copy link
                                  </Button>
                                  <Button
                                    disabled={!invitation.canRevoke || isAnyMutationPending}
                                    onClick={() => openDeleteInvitationDrawer(invitation)}
                                    type="button"
                                    variant="ghost"
                                  >
                                    Delete
                                  </Button>
                                </div>
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableRoot>
                  )}
                </Card>

                <Card className="space-y-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <h3 className="text-sm font-semibold text-gray-900">Current members</h3>
                      <p className="mt-1 text-xs text-gray-500">
                        Update roles and remove access without leaving the members settings page.
                      </p>
                    </div>
                    <Badge variant="success">{currentMembers.length} members</Badge>
                  </div>

                  {currentMembers.length === 0 ? (
                    <Alert title="No members found" type="info">
                      This subscription does not have any members yet.
                    </Alert>
                  ) : (
                    <TableRoot>
                      <Table>
                        <TableHead>
                          <TableRow>
                            <TableHeaderCell>Member</TableHeaderCell>
                            <TableHeaderCell>Role</TableHeaderCell>
                            <TableHeaderCell>Joined</TableHeaderCell>
                            <TableHeaderCell>Actions</TableHeaderCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {currentMembers.map((member) => (
                            <TableRow key={member.userId}>
                              <TableCell className="font-medium text-gray-900">
                                <div className="flex flex-col">
                                  <span>{member.displayName || member.email}</span>
                                  <span className="text-xs font-normal text-gray-500">{member.email}</span>
                                </div>
                              </TableCell>
                              <TableCell>
                                <div className="flex items-center gap-2">
                                  <span>{getRoleLabel(member.role)}</span>
                                  {member.isCurrentUser ? (
                                    <Badge variant="neutral">You</Badge>
                                  ) : null}
                                </div>
                              </TableCell>
                              <TableCell>{formatDateTime(member.joinedAtUtc)}</TableCell>
                              <TableCell>
                                <div className="flex flex-wrap gap-2">
                                  <Button
                                    disabled={!member.canUpdateRole || isAnyMutationPending}
                                    onClick={() => openMemberRoleDrawer(member)}
                                    type="button"
                                    variant="secondary"
                                  >
                                    Change role
                                  </Button>
                                  <Button
                                    disabled={!member.canDelete || isAnyMutationPending}
                                    onClick={() => openDeleteMemberDrawer(member)}
                                    type="button"
                                    variant="ghost"
                                  >
                                    Delete
                                  </Button>
                                </div>
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableRoot>
                  )}
                </Card>
              </>
            ) : null}
          </div>
        </div>
      </section>

      <Drawer open={isInviteDrawerOpen} onOpenChange={setIsInviteDrawerOpen}>
        <DrawerContent>
          <DrawerHeader>
            <div>
              <DrawerTitle>Invite team member</DrawerTitle>
              <DrawerDescription>
                Send a reusable invitation link to an owner or member for this subscription.
              </DrawerDescription>
            </div>
          </DrawerHeader>
          <DrawerBody>
            <form className="space-y-6" onSubmit={(event) => void handleInviteCreate(event)}>
              <div className="space-y-2">
                <label className="text-sm font-medium text-gray-900" htmlFor="invite-team-member-email">
                  Email
                </label>
                <Input
                  id="invite-team-member-email"
                  type="email"
                  placeholder="colleague@company.com"
                  hasError={Boolean(inviteEmailError)}
                  {...invitationForm.register("email")}
                />
                {inviteEmailError ? (
                  <p className="text-xs text-red-500">{inviteEmailError}</p>
                ) : null}
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium text-gray-900" htmlFor="invite-team-member-role">
                  Role
                </label>
                <Controller
                  control={invitationForm.control}
                  name="role"
                  render={({ field }) => {
                    return (
                      <Select value={String(field.value)} onValueChange={(value) => field.onChange(Number(value))}>
                        <SelectTrigger
                          id="invite-team-member-role"
                          className="w-full"
                          hasError={Boolean(inviteRoleError)}
                        >
                          <SelectValue placeholder="Select role" />
                        </SelectTrigger>
                        <SelectContent>
                          {availableRoles.map((role) => (
                            <SelectItem key={role} value={String(role)}>
                              {getRoleLabel(role)}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    );
                  }}
                />
                {inviteRoleError ? (
                  <p className="text-xs text-red-500">{inviteRoleError}</p>
                ) : null}
              </div>
            </form>
          </DrawerBody>
          <DrawerFooter>
            <DrawerClose asChild>
              <Button onClick={closeInviteDrawer} type="button" variant="secondary">
                Cancel
              </Button>
            </DrawerClose>
            <Button
              isLoading={createInvitationMutation.isPending}
              onClick={() => void handleInviteCreate()}
              type="button"
              variant="primary"
            >
              Send invitation
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      <Drawer open={isDeleteInvitationDrawerOpen} onOpenChange={setIsDeleteInvitationDrawerOpen}>
        <DrawerContent>
          <DrawerHeader>
            <div>
              <DrawerTitle>Delete invitation</DrawerTitle>
              <DrawerDescription>
                Revoke this invitation and keep the audit trail without deleting the record.
              </DrawerDescription>
            </div>
          </DrawerHeader>
          <DrawerBody>
            <div className="space-y-4">
              <p className="text-sm text-gray-600">
                {selectedInvitation
                  ? `This will revoke the pending invitation for ${selectedInvitation.email}.`
                  : "This will revoke the selected invitation."}
              </p>
              {selectedInvitation ? (
                <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
                  <p className="text-sm font-semibold text-gray-900">{selectedInvitation.email}</p>
                  <p className="mt-1 text-xs text-gray-500">
                    {getRoleLabel(selectedInvitation.role)} invited {formatDateTime(selectedInvitation.sentAtUtc)}
                  </p>
                </div>
              ) : null}
            </div>
          </DrawerBody>
          <DrawerFooter>
            <DrawerClose asChild>
              <Button onClick={closeDeleteInvitationDrawer} type="button" variant="secondary">
                Cancel
              </Button>
            </DrawerClose>
            <Button
              isLoading={deleteInvitationMutation.isPending}
              onClick={() => void handleInvitationDelete()}
              type="button"
              variant="destructive"
            >
              Delete invitation
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      <Drawer open={isMemberRoleDrawerOpen} onOpenChange={setIsMemberRoleDrawerOpen}>
        <DrawerContent>
          <DrawerHeader>
            <div>
              <DrawerTitle>Change member role</DrawerTitle>
              <DrawerDescription>
                Update the subscription role for this team member.
              </DrawerDescription>
            </div>
          </DrawerHeader>
          <DrawerBody>
            <form className="space-y-6" onSubmit={(event) => void handleMemberRoleSave(event)}>
              <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
                <p className="text-sm font-semibold text-gray-900">{selectedMember?.displayName || selectedMember?.email || "Member"}</p>
                <p className="mt-1 text-xs text-gray-500">{selectedMember?.email ?? "No email available"}</p>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium text-gray-900" htmlFor="member-role-select">
                  Role
                </label>
                <Controller
                  control={memberRoleForm.control}
                  name="role"
                  render={({ field }) => {
                    return (
                      <Select value={String(field.value)} onValueChange={(value) => field.onChange(Number(value))}>
                        <SelectTrigger
                          id="member-role-select"
                          className="w-full"
                          hasError={Boolean(memberRoleError)}
                        >
                          <SelectValue placeholder="Select role" />
                        </SelectTrigger>
                        <SelectContent>
                          {availableRoles.map((role) => (
                            <SelectItem key={role} value={String(role)}>
                              {getRoleLabel(role)}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    );
                  }}
                />
                {memberRoleError ? (
                  <p className="text-xs text-red-500">{memberRoleError}</p>
                ) : null}
              </div>
            </form>
          </DrawerBody>
          <DrawerFooter>
            <DrawerClose asChild>
              <Button onClick={closeMemberRoleDrawer} type="button" variant="secondary">
                Cancel
              </Button>
            </DrawerClose>
            <Button
              isLoading={updateMemberRoleMutation.isPending}
              onClick={() => void handleMemberRoleSave()}
              type="button"
              variant="primary"
            >
              Save role
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      <Drawer open={isDeleteMemberDrawerOpen} onOpenChange={setIsDeleteMemberDrawerOpen}>
        <DrawerContent>
          <DrawerHeader>
            <div>
              <DrawerTitle>Delete member</DrawerTitle>
              <DrawerDescription>
                Remove this user from the current subscription while preserving the rest of their account.
              </DrawerDescription>
            </div>
          </DrawerHeader>
          <DrawerBody>
            <div className="space-y-4">
              <p className="text-sm text-gray-600">
                {selectedMember
                  ? `This will remove ${selectedMember.email} from the subscription.`
                  : "This will remove the selected member from the subscription."}
              </p>
              {selectedMember ? (
                <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
                  <p className="text-sm font-semibold text-gray-900">{selectedMember.displayName || selectedMember.email}</p>
                  <p className="mt-1 text-xs text-gray-500">
                    {getRoleLabel(selectedMember.role)} joined {formatDateTime(selectedMember.joinedAtUtc)}
                  </p>
                </div>
              ) : null}
            </div>
          </DrawerBody>
          <DrawerFooter>
            <DrawerClose asChild>
              <Button onClick={closeDeleteMemberDrawer} type="button" variant="secondary">
                Cancel
              </Button>
            </DrawerClose>
            <Button
              isLoading={deleteMemberMutation.isPending}
              onClick={() => void handleMemberDelete()}
              type="button"
              variant="destructive"
            >
              Delete member
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>
    </>
  );
}
