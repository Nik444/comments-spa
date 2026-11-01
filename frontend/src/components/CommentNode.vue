<script setup lang="ts">
import { ref } from 'vue'
import type { CommentDto } from '@/api/types'
import CommentForm from '@/components/CommentForm.vue'
import CommentMeta from '@/components/CommentMeta.vue'
import { renderCommentHtml } from '@/utils/sanitize'
import type { ReplyNode } from '@/utils/tree'

defineProps<{ node: ReplyNode; depth: number }>()
const emit = defineEmits<{ created: [comment: CommentDto] }>()

const replying = ref(false)

function onCreated(comment: CommentDto) {
  replying.value = false
  emit('created', comment)
}
</script>

<template>
  <div class="reply" :class="{ 'reply-flat': depth >= 6 }">
    <CommentMeta
      :user-name="node.comment.userName"
      :email="node.comment.email"
      :home-page="node.comment.homePage"
      :created-at="node.comment.createdAt"
    />
    <div class="comment-text" v-html="renderCommentHtml(node.comment.text)" />

    <button v-if="!replying" type="button" class="btn link" @click="replying = true">Ответить</button>
    <CommentForm v-else :parent-id="node.comment.id" @created="onCreated" @cancel="replying = false" />

    <CommentNode v-for="child in node.children" :key="child.comment.id" :node="child" :depth="depth + 1" @created="emit('created', $event)" />
  </div>
</template>
